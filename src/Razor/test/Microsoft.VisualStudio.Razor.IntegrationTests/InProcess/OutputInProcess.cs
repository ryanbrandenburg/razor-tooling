// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Razor.IntegrationTests.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    [TestService]
    internal partial class OutputInProcess
    {
        private const string RazorPaneName = "Razor Language Server Client";

        public async Task<bool> HasErrorsAsync(CancellationToken cancellationToken)
        {
            var content = await GetRazorOutputPaneContentAsync(cancellationToken);

            return content is null || content.Contains("Error");
        }

        /// <summary>
        /// This method returns the current content of the "Razor Language Server Client" output pane.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The contents of the RLSC output pane.</returns>
        public async Task<string?> GetRazorOutputPaneContentAsync(CancellationToken cancellationToken)
        {
            await WaitForOutputPaneAsync(RazorPaneName, cancellationToken);
            var outputPaneTextView = GetOutputPaneTextView(RazorPaneName);

            if (outputPaneTextView is null)
            {
                return null;
            }

            return await outputPaneTextView.GetContentAsync(JoinableTaskFactory, cancellationToken);
        }

        public Task WaitForRazorOutputPaneAsync(CancellationToken cancellationToken) => WaitForOutputPaneAsync(RazorPaneName, cancellationToken);

        public async Task WaitForOutputPaneAsync(string paneName, CancellationToken cancellationToken)
        {
            var (outputWindow, _) = GetOutputWindow();

            using var semaphore = new SemaphoreSlim(1);
            await semaphore.WaitAsync(cancellationToken);
            var outputWindowEvents = outputWindow.OutputWindowPanes.DTE.Events.OutputWindowEvents;
            outputWindowEvents.PaneAdded += On_PaneAdded;

            var outputPane = GetOutputPaneTextView(paneName);
            if(outputPane is not null)
            {
                semaphore.Release();
                outputWindowEvents.PaneAdded -= On_PaneAdded;
                return;
            }

            try
            {
                await semaphore.WaitAsync(cancellationToken);
            }
            finally
            {
                outputWindowEvents.PaneAdded -= On_PaneAdded;
            }

            void On_PaneAdded(OutputWindowPane outputPane)
            {
                if (outputPane.Name.Equals(paneName))
                {
                    semaphore.Release();
                }
            }
        }

        private static IVsTextView? GetOutputPaneTextView(string paneName)
        {
            var (outputWindow, sVSOutputWindow) = GetOutputWindow();

            // This is a public entry point to COutputWindow::GetPaneByName
            EnvDTE.OutputWindowPane? pane = null;
            try
            {
                pane = outputWindow.OutputWindowPanes.Item(paneName);
            }
            catch (ArgumentException)
            {
                return null;
            }

            var textView = OutputWindowPaneToIVsTextView(pane, sVSOutputWindow);

            return textView;

            static IVsTextView OutputWindowPaneToIVsTextView(EnvDTE.OutputWindowPane outputWindowPane, IVsOutputWindow sVsOutputWindow)
            {
                var guid = Guid.Parse(outputWindowPane.Guid);
                ErrorHandler.ThrowOnFailure(sVsOutputWindow.GetPane(guid, out var result));

                if (result is not IVsTextView textView)
                {
                    throw new InvalidOperationException($"{nameof(IVsOutputWindowPane)} should implement {nameof(IVsTextView)}");
                }

                return textView;
            }
        }

        private static OutputWindowParts GetOutputWindow()
        {
            var sVSOutputWindow = ServiceProvider.GlobalProvider.GetService<SVsOutputWindow, IVsOutputWindow>();
            var extensibleObject = ServiceProvider.GlobalProvider.GetService<SVsOutputWindow, IVsExtensibleObject>();

            // The null propName gives us the OutputWindow object
            ErrorHandler.ThrowOnFailure(extensibleObject.GetAutomationObject(pszPropName: null, out var outputWindowObj));
            var outputWindow = (EnvDTE.OutputWindow)outputWindowObj;

            return new OutputWindowParts(outputWindow, sVSOutputWindow);
        }

        private record OutputWindowParts(OutputWindow OutputWindow, IVsOutputWindow SVsOutputWindow);
    }
}
