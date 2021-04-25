// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models;
using OmniSharp.Extensions.JsonRpc;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.EndpointContracts
{
    [Serial, Method("fileSystem/watch", Direction.ClientToServer)]
    internal interface IFileSystemWatchHandler : IJsonRpcNotificationHandler<WatchParams>
    {
    }
}
