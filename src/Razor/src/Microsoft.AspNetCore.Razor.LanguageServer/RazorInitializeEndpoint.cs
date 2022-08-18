// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    [LanguageServerEndpoint(Methods.InitializeName)]
    internal class RazorInitializeEndpoint : IRazorRequestHandler<InitializeParams, InitializeResult>
    {
        public bool MutatesSolutionState => true;

        public object? GetTextDocumentIdentifier(InitializeParams request)
        {
            return null;
        }

        public Task<InitializeResult> HandleRequestAsync(InitializeParams request, RazorRequestContext context, CancellationToken cancellationToken)
        {
            var capabilitiesManager = context.GetRequiredService<IInitializeManager<InitializeParams, InitializeResult>>();

            capabilitiesManager.SetInitializeParams(request);
            var serverCapabilities = capabilitiesManager.GetInitializeResult();

            return Task.FromResult(serverCapabilities);
        }
    }
}
