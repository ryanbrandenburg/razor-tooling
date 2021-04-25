// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    /**
     * An event to signal that a resource has been created, changed, or deleted. This
     * event should fire for resources that are being [watched](#FileSystemProvider.watch)
     * by clients of this provider.
     *
     * *Note:* It is important that the metadata of the file that changed provides an
     * updated `mtime` that advanced from the previous value in the [stat](#FileStat) and a
     * correct `size` value. Otherwise there may be optimizations in place that will not show
     * the change in an editor for example.
     */
    internal class DidChangeFileParams
    {
        /**
         * The change events.'
         */
        public FileChangeEvent[] Changes { get; set; }
    }
}
