﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;

[LanguageServerEndpoint(Methods.TextDocumentRenameName)]
internal interface IRenameEndpoint : IRazorRequestHandler<RenameParamsBridge, WorkspaceEdit?>, IRegistrationExtension
{
}
