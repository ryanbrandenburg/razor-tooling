// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    /**
     * Enumeration of file change types.
     */
    internal enum FileChangeType
    {
        /**
         * The contents or metadata of a file have changed.
         */
        Changed = 1,

        /**
         * A file has been created.
         */
        Created = 2,

        /**
         * A file has been deleted.
         */
        Deleted = 3,
    }
}
