// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class FileStatResponse : FileStat
    {
        public FileStatResponse(FileStat fileStat)
        {
            Type = fileStat.Type;
            Ctime = fileStat.Ctime;
            Mtime = fileStat.Mtime;
            Size = fileStat.Size;
        }
    }
}
