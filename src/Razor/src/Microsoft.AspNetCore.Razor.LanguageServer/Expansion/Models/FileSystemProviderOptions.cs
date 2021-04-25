// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class FileSystemProviderOptions
    {
        /**
         * The uri-scheme the provider registers for
         */
        public string Scheme { get; set; }

        /**
         * Whether or not the file system is case sensitive.
         */
        public bool? IsCaseSensitive { get; set; }

        /**
         * Whether or not the file system is readonly.
         */
        public bool? IsReadonly { get; set; }
    }
}
