// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion
{
    internal abstract class FileSystemProvider : ProjectSnapshotChangeTrigger
    {
        public abstract void Watch(Uri uri, string subsriptionId, WatchFileOptions options);

        public abstract void StopWatching(string subscriptionId);

        public abstract Task<FileStat> StatAsync(Uri uri, CancellationToken cancellationToken);

        public abstract Task<IReadOnlyList<DirectoryChild>> ReadDirectoryAsync(Uri uri, CancellationToken cancellationToken);

        public abstract Task CreateDirectoryAsync(Uri uri, CancellationToken cancellationToken);

        public abstract Task<string> ReadFileAsync(Uri uri, CancellationToken cancellationToken);

        public abstract Task WriteFileAsync(Uri uri, string content, WriteFileOptions options, CancellationToken cancellationToken);

        public abstract Task DeleteAsync(Uri uri, DeleteFileOptions options, CancellationToken cancellationToken);

        public abstract Task RenameAsync(Uri oldUri, Uri newUri, RenameFileOptions options, CancellationToken cancellationToken);
    }
}
