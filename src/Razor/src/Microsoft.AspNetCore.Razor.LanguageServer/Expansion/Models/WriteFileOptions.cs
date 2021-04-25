// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class WriteFileOptions
    {
        /**
         * If a new file should be created.
         */
        public bool Create { get; set; }

        /**
         * If a pre-existing file should be overwritten.
         */
        public bool Overwrite { get; set; }
    }
}
