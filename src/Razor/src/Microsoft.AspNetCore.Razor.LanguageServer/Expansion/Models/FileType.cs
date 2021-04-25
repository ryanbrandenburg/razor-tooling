// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion
{
    /**
     * Enumeration of file types. The types `File` and `Directory` can also be
     * a symbolic links, in that case use `FileType.File | FileType.SymbolicLink` and
     * `FileType.Directory | FileType.SymbolicLink`.
     */
    internal enum FileType
    {
        /**
         * The file type is unknown.
         */
        Unknown = 0,

        /**
         * A regular file.
         */
        File = 1,

        /**
         * A directory.
         */
        Directory = 2,

        /**
         * A symbolic link to a file or folder
         */
        Symbolic = 64,
    }
}
