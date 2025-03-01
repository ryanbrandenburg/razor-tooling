﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class RazorDocumentRangeFormattingEndpoint : IVSDocumentRangeFormattingEndpoint
    {
        private readonly DocumentContextFactory _documentContextFactory;
        private readonly RazorFormattingService _razorFormattingService;
        private readonly IOptionsMonitor<RazorLSPOptions> _optionsMonitor;

        public RazorDocumentRangeFormattingEndpoint(
            DocumentContextFactory documentContextFactory,
            RazorFormattingService razorFormattingService,
            IOptionsMonitor<RazorLSPOptions> optionsMonitor)
        {
            if (documentContextFactory is null)
            {
                throw new ArgumentNullException(nameof(documentContextFactory));
            }

            if (razorFormattingService is null)
            {
                throw new ArgumentNullException(nameof(razorFormattingService));
            }

            if (optionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            _documentContextFactory = documentContextFactory;
            _razorFormattingService = razorFormattingService;
            _optionsMonitor = optionsMonitor;
        }

        public RegistrationExtensionResult? GetRegistration(VSInternalClientCapabilities clientCapabilities)
        {
            const string ServerCapability = "documentRangeFormattingProvider";

            return new RegistrationExtensionResult(ServerCapability, new DocumentRangeFormattingOptions());
        }

        public async Task<TextEdit[]?> Handle(DocumentRangeFormattingParamsBridge request, CancellationToken cancellationToken)
        {
            if (!_optionsMonitor.CurrentValue.EnableFormatting)
            {
                return null;
            }

            var documentContext = await _documentContextFactory.TryCreateAsync(request.TextDocument.Uri, cancellationToken).ConfigureAwait(false);
            if (documentContext is null || cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            var codeDocument = await documentContext.GetCodeDocumentAsync(cancellationToken);
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            var edits = await _razorFormattingService.FormatAsync(request.TextDocument.Uri, documentContext.Snapshot, request.Range, request.Options, cancellationToken);

            return edits;
        }
    }
}
