// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using CommonLanguageServerProtocol.Framework;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    [LanguageServerEndpoint(LanguageServerConstants.RazorMonitorProjectConfigurationFilePathEndpoint)]
    internal interface IMonitorProjectConfigurationFilePathHandler : IRazorNotificationHandler<MonitorProjectConfigurationFilePathParams>
    {
    }
}
