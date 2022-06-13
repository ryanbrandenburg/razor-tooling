// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.AspNetCore.Razor.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.AspNetCore.Razor.LanguageServer.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Hover
{
    internal class RazorHoverEndpoint : AbstractRazorDelegatingEndpoint<VSHoverParamsBridge, VSInternalHover>, IVSHoverEndpoint
    {
        private readonly RazorHoverInfoService _hoverInfoService;
        private readonly RazorDocumentMappingService _documentMappingService;
        private VSInternalClientCapabilities? _clientCapabilities;

        [ImportingConstructor]
        public RazorHoverEndpoint(
            RazorHoverInfoService hoverInfoService,
            LanguageServerFeatureOptions languageServerFeatureOptions,
            RazorDocumentMappingService documentMappingService,
            ClientNotifierServiceBase languageServer,
            ILoggerFactory loggerFactory)
            : base(languageServerFeatureOptions, documentMappingService, languageServer, loggerFactory.CreateLogger<RazorHoverEndpoint>())
        {
            _hoverInfoService = hoverInfoService ?? throw new ArgumentNullException(nameof(hoverInfoService));
            _documentMappingService = documentMappingService ?? throw new ArgumentNullException(nameof(documentMappingService));
        }

        public RegistrationExtensionResult? GetRegistration(ClientCapabilities clientCapabilities)
        {
            const string AssociatedServerCapability = "hoverProvider";
            _clientCapabilities = clientCapabilities.ToVSInternalClientCapabilities();

            var registrationOptions = new HoverOptions()
            {
                WorkDoneProgress = false,
            };

            return new RegistrationExtensionResult(AssociatedServerCapability, new SumType<bool, HoverOptions>(registrationOptions));
        }

        /// <inheritdoc/>
        protected override string CustomMessageTarget => RazorLanguageServerCustomMessageTargets.RazorHoverEndpointName;

        public override bool MutatesSolutionState => false;

        /// <inheritdoc/>
<<<<<<< HEAD
        protected override IDelegatedParams CreateDelegatedParams(VSHoverParamsBridge request, RazorRequestContext razorRequestContext, Projection projection, CancellationToken cancellationToken)
        {
            razorRequestContext.RequireDocumentContext();
            var documentContext = razorRequestContext.DocumentContext;
            return new DelegatedPositionParams(
                    documentContext.Identifier,
                    projection.Position,
                    projection.LanguageKind);
        }

        /// <inheritdoc/>
        protected override async Task<VSInternalHover?> TryHandleAsync(VSHoverParamsBridge request, RazorRequestContext razorRequestContext, Projection projection, CancellationToken cancellationToken)
        {
            // HTML can still sometimes be handled by razor. For example hovering over
            // a component tag like <Counter /> will still be in an html context
            if (projection.LanguageKind == RazorLanguageKind.CSharp)
            {
                return null;
            }

            var location = new SourceLocation(projection.AbsoluteIndex, request.Position.Line, request.Position.Character);
            var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken);

            return _hoverInfoService.GetHoverInfo(codeDocument, location, _clientCapabilities!);
        }

        /// <inheritdoc/>
        protected override async Task<VSInternalHover?> HandleDelegatedResponseAsync(VSInternalHover? response, RazorRequestContext razorRequestContext, CancellationToken cancellationToken)
        {
            if (response?.Range is null)
            {
                return response;
            }

            razorRequestContext.RequireDocumentContext();
            var documentContext = razorRequestContext.DocumentContext;
            var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken).ConfigureAwait(false);

            if (_documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, response.Range, out var projectedRange))
            {
                response.Range = projectedRange;
            }

            return response;
        }
    }
}
