﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed
{
    [Shared]
    [ExportWorkspaceServiceFactory(typeof(TagHelperResolver), ServiceLayer.Default)]
    internal class ExportedTagHelperResolverFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new DefaultTagHelperResolver();
        }
    }
}
