// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class WatchFileOptions
    {
        /**
         * If a folder should be recursively subscribed to
         */
        public bool Recursive { get; set; }

        /**
         * Folders or files to exclude from being watched.
         */
        public string[] Excludes { get; set; }
    }
}
