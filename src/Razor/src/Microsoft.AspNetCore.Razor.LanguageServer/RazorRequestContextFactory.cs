// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorRequestContextFactory : IRequestContextFactory<RazorRequestContext>
    {
        private readonly ILspServices _lspServices;

        public RazorRequestContextFactory(ILspServices lspServices)
        {
            _lspServices = lspServices;
        }

        public async Task<RazorRequestContext> CreateRequestContextAsync(IQueueItem<RazorRequestContext> queueItem, CancellationToken cancellationToken)
        {
            DocumentContext? documentContext = null;
            if (queueItem.TextDocument is not null)
            {
                var textDocumentUri = queueItem.TextDocument switch
                {
                    TextDocumentIdentifier identifier => identifier.Uri,
                    Uri uri => uri,
                    _ => throw new NotImplementedException(),
                };
                var documentContextFactory = _lspServices.GetRequiredService<DocumentContextFactory>();
                documentContext = await documentContextFactory.TryCreateAsync(textDocumentUri, cancellationToken);
            }

            var lspLogger = _lspServices.GetRequiredService<ILspLogger>();
            var loggerFactory = _lspServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger(queueItem.MethodName);

            var requestContext = new RazorRequestContext(documentContext, lspLogger, logger, _lspServices);

            return requestContext;
        }
    }
}
