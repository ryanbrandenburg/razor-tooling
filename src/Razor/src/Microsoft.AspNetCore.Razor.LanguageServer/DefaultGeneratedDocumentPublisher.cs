// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Expansion;
using Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultGeneratedDocumentPublisher : GeneratedDocumentPublisher
    {
        private readonly object _publishLock = new object();
        private readonly Dictionary<string, PublishData> _publishedCSharpData;
        private readonly Dictionary<string, PublishData> _publishedHtmlData;
        private readonly IClientLanguageServer _server;
        private readonly ILogger _logger;
        private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
        private ProjectSnapshotManagerBase _projectSnapshotManager;

        public DefaultGeneratedDocumentPublisher(
            ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher,
            IClientLanguageServer server,
            ILoggerFactory loggerFactory)
        {
            if (projectSnapshotManagerDispatcher is null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerDispatcher));
            }

            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
            _server = server;
            _logger = loggerFactory.CreateLogger<DefaultGeneratedDocumentPublisher>();
            _publishedCSharpData = new Dictionary<string, PublishData>(FilePathComparer.Instance);
            _publishedHtmlData = new Dictionary<string, PublishData>(FilePathComparer.Instance);
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectSnapshotManager = projectManager;
            _projectSnapshotManager.Changed += ProjectSnapshotManager_Changed;
        }

        public override async Task PublishGeneratedDocumentsAsync(string filePath, RazorCodeDocument codeDocument, int hostDocumentVersion, CancellationToken cancellationToken)
        {
            // TODO: NOTHING IS SYNCHRONIZED AHHHH

            var shouldOpenDocument = !_publishedCSharpData.ContainsKey(filePath);
            var textDocumentEdits = new List<WorkspaceEditDocumentChange>();

            var csharpSourceText = codeDocument.GetCSharpSourceText();
            lock (_publishLock)
            {
                var csharpTextChanges = PreparePublishCSharp(filePath, csharpSourceText, hostDocumentVersion);
                var csharpFilePath = "/" + filePath + RazorServerLSPConstants.VirtualCSharpFileNameSuffix;
                TryAddDocumentEdit(csharpFilePath, csharpTextChanges, csharpSourceText, hostDocumentVersion, textDocumentEdits);
            }

            lock (_publishedHtmlData)
            {
                var htmlSourceText = codeDocument.GetHtmlSourceText();
                var htmlTextChanges = PreparePublishHtml(filePath, htmlSourceText, hostDocumentVersion);
                var htmlFilePath = "/" + filePath + RazorServerLSPConstants.VirtualHtmlFileNameSuffix;
                TryAddDocumentEdit(htmlFilePath, htmlTextChanges, htmlSourceText, hostDocumentVersion, textDocumentEdits);
            }

            if (textDocumentEdits.Count == 0)
            {
                // No workspace edits, no need to do anything.
                return;
            }

            if (shouldOpenDocument)
            {
                // Need to open the document instead of doing a workspace edit
                await InitializeVirtualDocumentsAsync(filePath, cancellationToken);
                return;
            }

            var parameters = new ApplyWorkspaceEditParams()
            {
                Edit = new WorkspaceEdit()
                {
                    DocumentChanges = textDocumentEdits
                },
            };

            var request = _server.SendRequest("workspace/applyEdit2", parameters);
            var response = await request.Returning<ApplyWorkspaceEditResponse>(cancellationToken);
            if (!response.Applied)
            {
                Debugger.Launch();
                Debugger.Break();
                Debug.Fail("Failed to apply workspace Edit, uh oh!");
            }

            static void TryAddDocumentEdit(
                string filePath,
                IReadOnlyList<TextChange> textChanges,
                SourceText sourceText,
                int hostDocumentVersion,
                List<WorkspaceEditDocumentChange> documentEdits)
            {
                if (textChanges is null || textChanges.Count == 0)
                {
                    return;
                }

                var documentUri = new DocumentUri(RazorServerLSPConstants.EmbeddedFileScheme, authority: string.Empty, path: filePath, query: string.Empty, fragment: string.Empty);
                var textEdits = textChanges.Select(change => change.AsTextEdit(sourceText));
                documentEdits.Add(new TextDocumentEdit()
                {
                    Edits = new TextEditContainer(textEdits),
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Uri = documentUri,
                        Version = hostDocumentVersion
                    }
                });
            }
        }

        public override void PublishCSharp(string filePath, SourceText sourceText, int hostDocumentVersion)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (sourceText is null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            _projectSnapshotManagerDispatcher.AssertDispatcherThread();

            var textChanges = PreparePublishCSharp(filePath, sourceText, hostDocumentVersion);
            if (textChanges is null)
            {
                return;
            }

            var request = new UpdateBufferRequest()
            {
                HostDocumentFilePath = filePath,
                Changes = textChanges,
                HostDocumentVersion = hostDocumentVersion,
            };
            var result = _server.SendRequest(LanguageServerConstants.RazorUpdateCSharpBufferEndpoint, request);
            // This is the call that actually makes the request, any SendRequest without a .Returning* after it will do nothing.
            _ = result.ReturningVoid(CancellationToken.None);
        }

        public override void PublishHtml(string filePath, SourceText sourceText, int hostDocumentVersion)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (sourceText is null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            _foregroundDispatcher.AssertForegroundThread();

            var textChanges = PreparePublishHtml(filePath, sourceText, hostDocumentVersion);
            if (textChanges is null)
            {
                return;
            }

            var request = new UpdateBufferRequest()
            {
                HostDocumentFilePath = filePath,
                Changes = textChanges,
                HostDocumentVersion = hostDocumentVersion,
            };

            var result = _server.SendRequest(LanguageServerConstants.RazorUpdateHtmlBufferEndpoint, request);
            _ = result.ReturningVoid(CancellationToken.None);
        }

        private IReadOnlyList<TextChange> PreparePublishCSharp(string filePath, SourceText sourceText, int hostDocumentVersion)
        {
            if (!_publishedCSharpData.TryGetValue(filePath, out var previouslyPublishedData))
            {
                previouslyPublishedData = PublishData.Default;
            }

            var textChanges = SourceTextDiffer.GetMinimalTextChanges(previouslyPublishedData.SourceText, sourceText);
            if (textChanges.Count == 0 && hostDocumentVersion == previouslyPublishedData.HostDocumentVersion)
            {
                // Source texts match along with host document versions. We've already published something that looks like this. No-op.
                return null;
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var previousDocumentLength = previouslyPublishedData.SourceText.Length;
                var currentDocumentLength = sourceText.Length;
                var documentLengthDelta = sourceText.Length - previousDocumentLength;
                _logger.LogTrace(
                    "Updating C# buffer of {0} to correspond with host document version {1}. {2} -> {3} = Change delta of {4} via {5} text changes.",
                    filePath,
                    hostDocumentVersion,
                    previousDocumentLength,
                    currentDocumentLength,
                    documentLengthDelta,
                    textChanges.Count);
            }

            _publishedCSharpData[filePath] = new PublishData(sourceText, hostDocumentVersion);

            return textChanges;
        }

        private IReadOnlyList<TextChange> PreparePublishHtml(string filePath, SourceText sourceText, int hostDocumentVersion)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (sourceText is null)
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            _projectSnapshotManagerDispatcher.AssertDispatcherThread();

            if (!_publishedHtmlData.TryGetValue(filePath, out var previouslyPublishedData))
            {
                previouslyPublishedData = PublishData.Default;
            }

            var textChanges = SourceTextDiffer.GetMinimalTextChanges(previouslyPublishedData.SourceText, sourceText);
            if (textChanges.Count == 0 && hostDocumentVersion == previouslyPublishedData.HostDocumentVersion)
            {
                // Source texts match along with host document versions. We've already published something that looks like this. No-op.
                return null;
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var previousDocumentLength = previouslyPublishedData.SourceText.Length;
                var currentDocumentLength = sourceText.Length;
                var documentLengthDelta = sourceText.Length - previousDocumentLength;
                _logger.LogTrace(
                    "Updating HTML buffer of {0} to correspond with host document version {1}. {2} -> {3} = Change delta of {4} via {5} text changes.",
                    filePath,
                    hostDocumentVersion,
                    previousDocumentLength,
                    currentDocumentLength,
                    documentLengthDelta,
                    textChanges.Count);
            }

            _publishedHtmlData[filePath] = new PublishData(sourceText, hostDocumentVersion);

            return textChanges;
        }

        private void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            // Don't do any work if the solution is closing
            if (args.SolutionIsClosing)
            {
                return;
            }

            _projectSnapshotManagerDispatcher.AssertDispatcherThread();

            switch (args.Kind)
            {
                case ProjectChangeKind.DocumentChanged:
                    if (!_projectSnapshotManager.IsDocumentOpen(args.DocumentFilePath))
                    {
                        // Document closed, evict published source text.
                        if (_publishedCSharpData.ContainsKey(args.DocumentFilePath))
                        {
                            var removed = _publishedCSharpData.Remove(args.DocumentFilePath);
                            Debug.Assert(removed, "Published data should be protected by the project snapshot manager's thread and should never fail to remove.");
                        }
                        if (_publishedHtmlData.ContainsKey(args.DocumentFilePath))
                        {
                            var removed = _publishedHtmlData.Remove(args.DocumentFilePath);
                            Debug.Assert(removed, "Published data should be protected by the project snapshot manager's thread and should never fail to remove.");
                        }
                    }
                    break;
            }
        }
        private async Task InitializeVirtualDocumentsAsync(string hostDocumentFilePath, CancellationToken cancellationToken)
        {
            var openCSharpTask = SendOpenTextDocumentAsync(hostDocumentFilePath, RazorServerLSPConstants.VirtualCSharpFileNameSuffix, cancellationToken);
            var openHtmlTask = SendOpenTextDocumentAsync(hostDocumentFilePath, RazorServerLSPConstants.VirtualHtmlFileNameSuffix, cancellationToken);
            await Task.WhenAll(openCSharpTask, openHtmlTask).ConfigureAwait(false);
        }

        private async Task SendOpenTextDocumentAsync(string hostDocumentFilePath, string virtualFileSuffix, CancellationToken cancellationToken)
        {
            var csharpFilePath = "/" + hostDocumentFilePath + virtualFileSuffix;
            var csharpDocumentUri = new DocumentUri(RazorServerLSPConstants.EmbeddedFileScheme, authority: string.Empty, path: csharpFilePath, query: string.Empty, fragment: string.Empty);
            var openDocumentParams = new OpenTextDocumentParams()
            {
                TextDocument = new TextDocumentIdentifier(csharpDocumentUri)
            };
            var request = this._server.SendRequest("textDocument/open", openDocumentParams);
            await request.ReturningVoid(cancellationToken);
        }

        private sealed class PublishData
        {
            public static readonly PublishData Default = new PublishData(SourceText.From(string.Empty), null);

            public PublishData(SourceText sourceText, int? hostDocumentVersion)
            {
                SourceText = sourceText;
                HostDocumentVersion = hostDocumentVersion;
            }

            public SourceText SourceText { get; }

            public int? HostDocumentVersion { get; }
        }
    }
}
