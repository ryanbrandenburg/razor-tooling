// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using CommonLanguageServerProtocol.Framework;
using Microsoft.AspNetCore.Razor.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts.Debugging;

[LanguageServerEndpoint(LanguageServerConstants.RazorProximityExpressionsEndpoint)]
internal interface IRazorProximityExpressionsEndpoint : IRazorRequestHandler<RazorProximityExpressionsParamsBridge, RazorProximityExpressionsResponse?>
{
}
