// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
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
}
