// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    /**
     * The event filesystem providers must use to signal a file change.
     */
    internal class FileChangeEvent
    {
        /**
         * The type of change.
         */
        public Uri Uri { get; set; }

        /**
         * The uri of the file that has changed.
         */
        public FileChangeType Type { get; set; }
    }
}
