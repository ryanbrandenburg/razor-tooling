// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Expansion;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using HoverModel = OmniSharp.Extensions.LanguageServer.Protocol.Models.Hover;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Hover
{
    internal class RazorHoverEndpoint : IHoverHandler
    {
        private readonly ILogger _logger;
        private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
        private readonly DocumentResolver _documentResolver;
        private readonly RazorHoverInfoService _hoverInfoService;
        private readonly ClientNotifierServiceBase _languageServer;
        private readonly IClientLanguageServer _server;
        private readonly RazorDocumentMappingService _documentMappingService;

        public RazorHoverEndpoint(
            ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher,
            DocumentResolver documentResolver,
            RazorHoverInfoService hoverInfoService,
            ClientNotifierServiceBase languageServer,
            IClientLanguageServer server,
            RazorDocumentMappingService documentMappingService,
            ILoggerFactory loggerFactory)
        {
            if (projectSnapshotManagerDispatcher is null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerDispatcher));
            }

            if (documentResolver is null)
            {
                throw new ArgumentNullException(nameof(documentResolver));
            }

            if (hoverInfoService is null)
            {
                throw new ArgumentNullException(nameof(hoverInfoService));
            }

            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (documentMappingService is null)
            {
                throw new ArgumentNullException(nameof(documentMappingService));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
            _documentResolver = documentResolver;
            _hoverInfoService = hoverInfoService;
            _languageServer = languageServer;
            _server = server;
            _documentMappingService = documentMappingService;
            _logger = loggerFactory.CreateLogger<RazorHoverEndpoint>();
        }

        public async Task<HoverModel> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var documentFilePath = request.TextDocument.Uri.GetAbsoluteOrUNCPath();
            var document = await _projectSnapshotManagerDispatcher.RunOnDispatcherThreadAsync(() =>
            {
                _documentResolver.TryResolveDocument(documentFilePath, out var documentSnapshot);

                return documentSnapshot;
            }, cancellationToken).ConfigureAwait(false);

            if (document is null)
            {
                return null;
            }

            var codeDocument = await document.GetGeneratedOutputAsync();
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            var sourceText = await document.GetTextAsync();
            var linePosition = new LinePosition((int)request.Position.Line, (int)request.Position.Character);
            var hostDocumentIndex = sourceText.Lines.GetPosition(linePosition);
            var location = new SourceLocation(hostDocumentIndex, (int)request.Position.Line, (int)request.Position.Character);
            var clientCapabilities = _languageServer.ClientSettings.Capabilities;

            var hoverItem = _hoverInfoService.GetHoverInfo(codeDocument, location, clientCapabilities);

            var languageKind = _documentMappingService.GetLanguageKind(codeDocument, hostDocumentIndex);
            if (languageKind == RazorLanguageKind.CSharp)
            {
                var delegatedHoverParams = request;
                var virtualFilePath = "/" + documentFilePath + RazorServerLSPConstants.VirtualCSharpFileNameSuffix;
                var virtualDocumentUri = new DocumentUri(RazorServerLSPConstants.EmbeddedFileScheme, authority: string.Empty, path: virtualFilePath, query: string.Empty, fragment: string.Empty);
                delegatedHoverParams.TextDocument.Uri = virtualDocumentUri;
                if (_documentMappingService.TryMapToProjectedDocumentPosition(codeDocument, hostDocumentIndex, out var projectedPosition, out var projectedIndex))
                {
                    delegatedHoverParams.Position = projectedPosition;
                }
                delegatedHoverParams.WorkDoneToken = null;
                var delegatedRequest = _server.SendRequest("textDocument/hover", delegatedHoverParams);
                var hoverModels = await delegatedRequest.Returning<HoverModel[]>(cancellationToken).ConfigureAwait(false);
                if (hoverModels != null && hoverModels.Length > 0)
                {
                    hoverItem = hoverModels[0];

                    if (hoverItem.Range != null &&
                        _documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, hoverItem.Range, out var mappedRange))
                    {
                        hoverItem.Range = mappedRange;
                    }
                }
            }
            else if (languageKind == RazorLanguageKind.Html)
            {
                var delegatedHoverParams = request;
                var virtualFilePath = "/" + documentFilePath + RazorServerLSPConstants.VirtualHtmlFileNameSuffix;
                var virtualDocumentUri = new DocumentUri(RazorServerLSPConstants.EmbeddedFileScheme, authority: string.Empty, path: virtualFilePath, query: string.Empty, fragment: string.Empty);
                delegatedHoverParams.TextDocument.Uri = virtualDocumentUri;
                if (_documentMappingService.TryMapToProjectedDocumentPosition(codeDocument, hostDocumentIndex, out var projectedPosition, out var projectedIndex))
                {
                    delegatedHoverParams.Position = projectedPosition;
                }
                delegatedHoverParams.WorkDoneToken = null;
                var delegatedRequest = _server.SendRequest("textDocument/hover", delegatedHoverParams);
                var hoverModels = await delegatedRequest.Returning<HoverModel[]>(cancellationToken).ConfigureAwait(false);
                if (hoverModels != null && hoverModels.Length > 0)
                {
                    hoverItem = hoverModels[0];
                }
            }

            _logger.LogTrace($"Found hover info items.");

            return hoverItem;
        }

        public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities)
        {
            return new HoverRegistrationOptions
            {
                DocumentSelector = RazorDefaults.Selector,
            };
        }
    }
}
