﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultWorkspaceDirectoryPathResolver : WorkspaceDirectoryPathResolver
    {
        private readonly IInitializeManager<InitializeParams, InitializeResult> _settingsManager;

        public DefaultWorkspaceDirectoryPathResolver(IInitializeManager<InitializeParams, InitializeResult> settingsManager)
        {
            if (settingsManager is null)
            {
                throw new ArgumentNullException(nameof(settingsManager));
            }

            _settingsManager = settingsManager;
        }

        public override string Resolve()
        {
            var clientSettings = _settingsManager.GetInitializeParams();
            if (clientSettings.RootUri is null)
            {
                // RootUri was added in LSP3, fallback to RootPath
#pragma warning disable CS0618 // Type or member is obsolete
                return clientSettings.RootPath!;
#pragma warning restore CS0618 // Type or member is obsolete
            }

            var normalized =  FilePathNormalizer.Instance.Normalize(clientSettings.RootUri);
            return normalized.AbsoluteUri;
        }
    }
}
