// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    /**
     * A name/type item that represents a directory child node.
     */
    internal class DirectoryChild
    {
        /**
         * The name of the node, e.g. a filename or directory name.
         */
        public string Name { get; set; }

        /**
         * The type of the file, e.g. is a regular file, a directory, or symbolic link to a file/directory.
         *
         * *Note:* This value might be a bitmask, e.g. `FileType.File | FileType.SymbolicLink`.
         */
        public FileType Type { get; set; }
    }
}
