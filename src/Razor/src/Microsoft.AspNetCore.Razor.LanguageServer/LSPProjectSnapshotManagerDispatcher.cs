// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class LSPProjectSnapshotManagerDispatcher : ProjectSnapshotManagerDispatcherBase
    {
        private const string ThreadName = "Razor." + nameof(LSPProjectSnapshotManagerDispatcher);

        [ImportingConstructor]
        public LSPProjectSnapshotManagerDispatcher() : base(ThreadName)
        {
        }

        public override void LogException(Exception ex) { }//=> _logger.LogError(ex, ThreadName + " encountered an exception.");
    }
}
