// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Host;
using Microsoft.Internal.VisualStudio.Shell.Embeddable.Feedback;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Razor.IntegrationTests.InProcess;
using Xunit.Harness;

namespace Microsoft.VisualStudio.Razor.IntegrationTests
{
    public abstract class AbstractRazorEditorTest : AbstractEditorTest
    {
        internal const string BlazorProjectName = "BlazorProject";

        private static readonly string s_pagesDir = Path.Combine("Pages");
        private static readonly string s_sharedDir = Path.Combine("Shared");
        internal static readonly string CounterRazorFile = Path.Combine(s_pagesDir, "Counter.razor");
        internal static readonly string IndexRazorFile = Path.Combine(s_pagesDir, "Index.razor");
        internal static readonly string SemanticTokensFile = Path.Combine(s_pagesDir, "SemanticTokens.razor");
        internal static readonly string MainLayoutFile = Path.Combine(s_sharedDir, "MainLayout.razor");
        internal static readonly string ErrorCshtmlFile = Path.Combine(s_pagesDir, "Error.cshtml");
        internal static readonly string ImportsRazorFile = "_Imports.razor";

        internal static readonly string MainLayoutContent = @"@inherits LayoutComponentBase

<PageTitle>BlazorApp</PageTitle>

<div class=""page"">
    <div class=""sidebar"">
        <NavMenu />
    </div>

    <main>
        <div class=""top-row px-4"">
            <a href=""https://docs.microsoft.com/aspnet/"" target=""_blank"">About</a>
        </div>

        <article class=""content px-4"">
            @Body
        </article>
    </main>
</div>
";

        private const string RazorOutputLogId = "RazorOutputLog";
        private const string LogHubLogId = "RazorLogHub";

        protected override string LanguageName => LanguageNames.Razor;

        private static bool s_customLoggersAdded = false;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            await TestServices.SolutionExplorer.CreateSolutionAsync("BlazorSolution", HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.AddProjectAsync("BlazorProject", WellKnownProjectTemplates.BlazorProject, groupId: WellKnownProjectTemplates.GroupIdentifiers.Server, templateId: null, LanguageName, HangMitigatingCancellationToken);
            await TestServices.SolutionExplorer.RestoreNuGetPackagesAsync(HangMitigatingCancellationToken);
            await TestServices.Workspace.WaitForProjectSystemAsync(HangMitigatingCancellationToken);

            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LanguageServer, HangMitigatingCancellationToken);

            // We open the Index.razor file, and wait for the SurveyPrompt component to be classified, as that
            // way we know the LSP server is up and running and responding
            await TestServices.SolutionExplorer.OpenFileAsync(BlazorProjectName, IndexRazorFile, HangMitigatingCancellationToken);
            await TestServices.Editor.WaitForClassificationAsync(HangMitigatingCancellationToken, expectedClassification: "RazorComponentElement");

            // Close the file we opened, just in case, so the test can start with a clean slate
            await TestServices.Editor.CloseDocumentWindowAsync(HangMitigatingCancellationToken);

            // Add custom logs on failure if they haven't already been.
            if (!s_customLoggersAdded)
            {
                DataCollectionService.RegisterCustomLogger(RazorOutputPaneLogger, RazorOutputLogId, "log");
                DataCollectionService.RegisterCustomLogger(RazorLogHubLogger, LogHubLogId, "zip");

                s_customLoggersAdded = true;
            }

            // Fun fact, LogHub logs don't get collected until the VS instance closes. So that's fun.
            async void RazorLogHubLogger(string filePath)
            {
                var componentModel = await TestServices.Shell.GetRequiredGlobalServiceAsync<SComponentModel, IComponentModel>(HangMitigatingCancellationToken);
                var feedbackFileProviders = componentModel.GetExtensions<IFeedbackDiagnosticFileProvider>();

                // Collect all the file names first since they can kick of file creation events that might need extra time to resolve.
                var files = new List<string>();
                foreach (var feedbackFileProvider in feedbackFileProviders)
                {
                    files.AddRange(feedbackFileProvider.GetFiles());
                }

                using var zip = ZipFile.Open(filePath, ZipArchiveMode.Create);
                foreach (var file in files)
                {
                    var name = Path.GetFileName(file);
                    // Files aren't guarenteed to exist once GetFiles returns, wait for some of the slower ones (like LogHub.zip)
                    WaitForFileExists(file);
                    if (File.Exists(file))
                    {
                        try
                        {
                            zip.CreateEntryFromFile(file, name);
                        }
                        catch (Exception)
                        {
                            // We can run into all kinds of file-related problems when doing this, so let's just avoid it all.
                        }
                    }
                }
            }

            async void RazorOutputPaneLogger(string filePath)
            {
                var paneContent = await TestServices.Output.GetRazorOutputPaneContentAsync(HangMitigatingCancellationToken);
                File.WriteAllText(filePath, paneContent);
            }
        }

        static void WaitForFileExists(string file)
        {
            const int MaxRetries = 120;
            var retries = 0;
            while (!File.Exists(file) && retries < MaxRetries)
            {
                retries++;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }
    }
}
