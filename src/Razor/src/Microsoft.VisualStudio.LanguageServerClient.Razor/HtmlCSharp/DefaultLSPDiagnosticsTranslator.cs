// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(LSPDiagnosticsTranslator))]
    internal class DefaultLSPDiagnosticsTranslator : LSPDiagnosticsTranslator
    {
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPRequestInvoker _requestInvoker;

        [ImportingConstructor]
        public DefaultLSPDiagnosticsTranslator(
            LSPDocumentManager documentManager,
            LSPRequestInvoker requestInvoker)
        {
            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            _documentManager = documentManager;
            _requestInvoker = requestInvoker;
        }

        public override async Task<RazorDiagnosticsResponse> TranslateAsync(
            RazorLanguageKind languageKind,
            Uri razorDocumentUri,
            Diagnostic[] diagnostics,
            CancellationToken cancellationToken)
        {
            if (razorDocumentUri is null)
            {
                throw new ArgumentNullException(nameof(razorDocumentUri));
            }

            if (diagnostics is null)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (!_documentManager.TryGetDocument(razorDocumentUri, out var documentSnapshot))
            {
                return new RazorDiagnosticsResponse()
                {
                    Diagnostics = Array.Empty<Diagnostic>(),
                };
            }

            var diagnosticsParams = new RazorDiagnosticsParams()
            {
                Kind = languageKind,
                RazorDocumentUri = razorDocumentUri,
                Diagnostics = diagnostics
            };

            var diagnosticResponse = await _requestInvoker.ReinvokeRequestOnServerAsync<RazorDiagnosticsParams, RazorDiagnosticsResponse>(
                documentSnapshot.Snapshot.TextBuffer,
                LanguageServerConstants.RazorTranslateDiagnosticsEndpoint,
                RazorLSPConstants.RazorLanguageServerName,
                diagnosticsParams,
                cancellationToken).ConfigureAwait(false);

            return diagnosticResponse.Result;
        }
    }
}
