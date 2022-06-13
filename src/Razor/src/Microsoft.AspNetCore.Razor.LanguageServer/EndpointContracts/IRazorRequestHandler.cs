// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts
{
    internal interface IRazorRequestHandler<RequestType, ResponseType> : IRequestHandler<RequestType, ResponseType, RazorRequestContext>
    {
    }
}
