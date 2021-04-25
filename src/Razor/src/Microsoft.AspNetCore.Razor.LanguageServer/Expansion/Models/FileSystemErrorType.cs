// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal enum FileSystemErrorType
    {
        /**
         * An error to signal that a file or folder wasn't found.
         */
        FileNotFound = 0,

        /**
         * An error to signal that a file or folder already exists, e.g. when creating but not overwriting a file.
         */
        FileExists = 1,

        /**
         * An error to signal that a file is not a folder.
         */
        FileNotADirectory = 2,

        /**
         * An error to signal that a file is a folder.
         */
        FileIsADirectory = 3,

        /**
         * An error to signal that an operation lacks required permissions.
         */
        NoPermissions = 4,

        /**
         * An error to signal that the file system is unavailable or too busy to complete a request.
         */
        Unavailable = 5,

        /**
         * A custom error.
         */
        Other = 1000,
    }
}
