// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorDidChangeTextDocumentEndpoint : IVSDidChangeTextDocumentEndpoint
    {
        public bool MutatesSolutionState => true;

        private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
        private readonly RazorProjectService _projectService;

        public RazorDidChangeTextDocumentEndpoint(ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher, RazorProjectService razorProjectService)
        {
            _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
            _projectService = razorProjectService;
        }

        public RegistrationExtensionResult? GetRegistration(ClientCapabilities clientCapabilities)
        {
            const string AssociatedServerCapability = "textDocumentSync";
            var registrationOptions = new TextDocumentSyncOptions()
            {
                Change = TextDocumentSyncKind.Incremental,
                OpenClose = true,
                Save = new SaveOptions()
                {
                    IncludeText = true,
                },
                WillSave = false,
                WillSaveWaitUntil = false,
            };

            var result = new RegistrationExtensionResult(AssociatedServerCapability, registrationOptions);

            return result;
        }

        public object? GetTextDocumentIdentifier(DidChangeTextDocumentParamsBridge request)
        {
            return request.TextDocument;
        }

        public async Task HandleNotificationAsync(DidChangeTextDocumentParamsBridge request, RazorRequestContext context, CancellationToken cancellationToken)
        {
            context.RequireDocumentContext();

            var sourceText = await context.DocumentContext.GetSourceTextAsync(cancellationToken);
            sourceText = ApplyContentChanges(request.ContentChanges, sourceText, context.LspLogger);

            await _projectSnapshotManagerDispatcher.RunOnDispatcherThreadAsync(
                () => _projectService.UpdateDocument(context.DocumentContext.FilePath, sourceText, request.TextDocument.Version),
                CancellationToken.None).ConfigureAwait(false);
        }

        // Internal for testing
        internal SourceText ApplyContentChanges(IEnumerable<TextDocumentContentChangeEvent> contentChanges, SourceText sourceText, ILspLogger logger)
        {
            foreach (var change in contentChanges)
            {
                if (change.Range is null)
                {
                    throw new ArgumentNullException(nameof(change.Range), "Range of change should not be null.");
                }

                var startLinePosition = new LinePosition(change.Range.Start.Line, change.Range.Start.Character);
                var startPosition = sourceText.Lines.GetPosition(startLinePosition);
                var endLinePosition = new LinePosition(change.Range.End.Line, change.Range.End.Character);
                var endPosition = sourceText.Lines.GetPosition(endLinePosition);

                var textSpan = new TextSpan(startPosition, change.RangeLength ?? endPosition - startPosition);
                var textChange = new TextChange(textSpan, change.Text);

                _ = logger.LogInformationAsync($"Applying {textChange}");

                // If there happens to be multiple text changes we generate a new source text for each one. Due to the
                // differences in VSCode and Roslyn's representation we can't pass in all changes simultaneously because
                // ordering may differ.
                sourceText = sourceText.WithChanges(textChange);
            }

            return sourceText;
        }
    }

    internal class RazorSaveTextDocumentEndpoint : IVSDidSaveTextDocumentEndpoint
    {
        public bool MutatesSolutionState => false;

        public object? GetTextDocumentIdentifier(DidSaveTextDocumentParamsBridge request)
        {
            return request.TextDocument;
        }

        public async Task HandleNotificationAsync(DidSaveTextDocumentParamsBridge request, RazorRequestContext context, CancellationToken cancellationToken)
        {
            await context.LspLogger.LogInformationAsync($"Saved Document {request.TextDocument.Uri.GetAbsoluteOrUNCPath()}");
        }
    }

    internal class RazorDidOpenTextDocumentEndpoint : IVSDidOpenTextDocumentEndpoint
    {
        public bool MutatesSolutionState => true;

        private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
        private readonly RazorProjectService _projectService;

        public RazorDidOpenTextDocumentEndpoint(ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher, RazorProjectService razorProjectService)
        {
            _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
            _projectService = razorProjectService;
        }

        public object? GetTextDocumentIdentifier(DidOpenTextDocumentParamsBridge request)
        {
            var identifier = new TextDocumentIdentifier
            {
                Uri = request.TextDocument.Uri,
            };
            return identifier;
        }

        public async Task HandleNotificationAsync(DidOpenTextDocumentParamsBridge request, RazorRequestContext context, CancellationToken cancellationToken)
        {
            var sourceText = SourceText.From(request.TextDocument.Text);

            await _projectSnapshotManagerDispatcher.RunOnDispatcherThreadAsync(
                () => _projectService.OpenDocument(request.TextDocument.Uri.GetAbsoluteOrUNCPath(), sourceText, request.TextDocument.Version),
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    internal class RazorDidCloseTextDocumentEndpoint : IVSDidCloseTextDocumentEndpoint
    {
        private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
        private readonly RazorProjectService _projectService;

        public bool MutatesSolutionState => true;

        public RazorDidCloseTextDocumentEndpoint(
            ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher,
            RazorProjectService projectService)
        {
            if (projectSnapshotManagerDispatcher is null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerDispatcher));
            }

            if (projectService is null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
            _projectService = projectService;
        }

        public object? GetTextDocumentIdentifier(DidCloseTextDocumentParamsBridge request)
        {
            return request.TextDocument;
        }

        public async Task HandleNotificationAsync(DidCloseTextDocumentParamsBridge request, RazorRequestContext context, CancellationToken cancellationToken)
        {
            await _projectSnapshotManagerDispatcher.RunOnDispatcherThreadAsync(
                () => _projectService.CloseDocument(request.TextDocument.Uri.GetAbsoluteOrUNCPath()),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
