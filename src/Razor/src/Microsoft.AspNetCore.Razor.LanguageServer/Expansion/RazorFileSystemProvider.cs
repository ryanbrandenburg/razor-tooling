// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion
{
    internal class RazorFileSystemProvider : FileSystemProvider
    {
        private Dictionary<string, File> _files = new Dictionary<string, File>(StringComparer.Ordinal);

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            projectManager.Changed += ProjectManager_Changed;
        }

        public override async Task<string> ReadFileAsync(Uri uri, CancellationToken cancellationToken)
        {
            var filePath = uri.GetAbsoluteOrUNCPath();
            if (!_files.TryGetValue(filePath, out var file))
            {
                throw new FileNotFoundException(filePath + " was not found.");
            }

            var content = await file.GetContentAsync().ConfigureAwait(false);
            return content;
        }

        public override async Task<FileStat> StatAsync(Uri uri, CancellationToken cancellationToken)
        {
            var filePath = uri.GetAbsoluteOrUNCPath();
            if (!_files.TryGetValue(filePath, out var file))
            {
                throw new FileNotFoundException(filePath + " was not found.");
            }

            var content = await file.GetStatAsync().ConfigureAwait(false);
            return content;
        }

        public override Task CreateDirectoryAsync(Uri uri, CancellationToken cancellationToken) => throw new NotImplementedException();

        public override Task DeleteAsync(Uri uri, DeleteFileOptions options, CancellationToken cancellationToken) => throw new NotImplementedException();

        public override Task<IReadOnlyList<DirectoryChild>> ReadDirectoryAsync(Uri uri, CancellationToken cancellationToken) => throw new NotImplementedException();

        public override Task RenameAsync(Uri oldUri, Uri newUri, RenameFileOptions options, CancellationToken cancellationToken) => throw new NotImplementedException();

        public override void StopWatching(string subscriptionId) => throw new NotImplementedException();

        public override void Watch(Uri uri, string subsriptionId, WatchFileOptions options) => throw new NotImplementedException();

        public override Task WriteFileAsync(Uri uri, string content, WriteFileOptions options, CancellationToken cancellationToken) => throw new NotImplementedException();

        private void ProjectManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            switch (args.Kind)
            {

                case ProjectChangeKind.ProjectChanged:
                    {
                        var projectSnapshot = args.Newer;
                        foreach (var documentFilePath in projectSnapshot.DocumentFilePaths)
                        {
                            var document = projectSnapshot.GetDocument(documentFilePath);
                            AddOrUpdateVirtualFiles(document);
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentAdded:
                    {
                        var projectSnapshot = args.Newer;
                        var document = projectSnapshot.GetDocument(args.DocumentFilePath);

                        // We don't enqueue the current document because added documents are by default closed.

                        foreach (var relatedDocument in projectSnapshot.GetRelatedDocuments(document))
                        {
                            AddOrUpdateVirtualFiles(relatedDocument);
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentChanged:
                    {
                        var projectSnapshot = args.Newer;
                        var document = projectSnapshot.GetDocument(args.DocumentFilePath);
                        AddOrUpdateVirtualFiles(document);

                        foreach (var relatedDocument in projectSnapshot.GetRelatedDocuments(document))
                        {
                            AddOrUpdateVirtualFiles(relatedDocument);
                        }

                        break;
                    }

                case ProjectChangeKind.DocumentRemoved:
                    {
                        var olderProject = args.Older;
                        var document = olderProject.GetDocument(args.DocumentFilePath);
                        RemoveVirtualFiles(document);

                        foreach (var relatedDocument in olderProject.GetRelatedDocuments(document))
                        {
                            var newerRelatedDocument = args.Newer.GetDocument(relatedDocument.FilePath);
                            AddOrUpdateVirtualFiles(newerRelatedDocument);
                        }
                        break;
                    }
                case ProjectChangeKind.ProjectRemoved:
                    {
                        var projectSnapshot = args.Older;
                        foreach (var documentFilePath in projectSnapshot.DocumentFilePaths)
                        {
                            var document = projectSnapshot.GetDocument(documentFilePath);
                            RemoveVirtualFiles(document);
                        }

                        break;
                    }
            }

            void RemoveVirtualFiles(DocumentSnapshot document)
            {
                var csharpFilePath = document.FilePath + RazorServerLSPConstants.VirtualCSharpFileNameSuffix;
                _files.Remove(csharpFilePath);
                var htmlFilePath = document.FilePath + RazorServerLSPConstants.VirtualHtmlFileNameSuffix;
                _files.Remove(htmlFilePath);
            }

            void AddOrUpdateVirtualFiles(DocumentSnapshot document)
            {
                var csharpFilePath = document.FilePath + RazorServerLSPConstants.VirtualCSharpFileNameSuffix;
                var htmlFilePath = document.FilePath + RazorServerLSPConstants.VirtualHtmlFileNameSuffix;

                int ctime;
                if (_files.TryGetValue(csharpFilePath, out var file))
                {
                    ctime = file.Ctime;
                }
                else
                {
                    // New file
                    ctime = ConvertToUnixTimestamp(DateTime.UtcNow);
                }

                var updatedCSharpFile = new CSharpFile(csharpFilePath, document, ctime);
                _files[csharpFilePath] = updatedCSharpFile;
                var updatedHtmlFile = new HtmlFile(htmlFilePath, document, ctime);
                _files[htmlFilePath] = updatedHtmlFile;
            }
        }

        private static int ConvertToUnixTimestamp(DateTime date)
        {
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var diff = date.ToUniversalTime() - origin;
            return (int)Math.Floor(diff.TotalMilliseconds);
        }

        internal abstract record Entry
        {
            public Entry(FileType fileType)
            {
                var mtime = ConvertToUnixTimestamp(DateTime.UtcNow);
                Mtime = mtime;
                FileType = fileType;
            }

            public int Mtime { get; }

            public FileType FileType { get; }
        }

        internal abstract record File(string FilePath, int Ctime) : Entry(FileType.File)
        {
            public abstract Task<string> GetContentAsync();

            public async Task<FileStat> GetStatAsync()
            {
                var content = await GetContentAsync().ConfigureAwait(false);

                var fileStat = CreateStat(content);
                return fileStat;
            }

            protected FileStat CreateStat(string content)
            {
                var size = Encoding.UTF8.GetByteCount(content);

                var fileStat = new FileStat()
                {
                    Type = FileType.File,
                    Ctime = Ctime,
                    Mtime = Mtime,
                    Size = size,
                };

                return fileStat;
            }
        }

        internal record CSharpFile(string FilePath, DocumentSnapshot Document, int Ctime) : File(FilePath, Ctime)
        {
            public override async Task<string> GetContentAsync()
            {
                var codeDocument = await Document.GetGeneratedOutputAsync().ConfigureAwait(false);
                var csharpDocument = codeDocument.GetCSharpDocument();
                return csharpDocument.GeneratedCode;
            }
        }

        internal record HtmlFile(string FilePath, DocumentSnapshot Document, int Ctime) : File(FilePath, Ctime)
        {
            public override async Task<string> GetContentAsync()
            {
                var codeDocument = await Document.GetGeneratedOutputAsync().ConfigureAwait(false);
                var csharpDocument = codeDocument.GetHtmlDocument();
                return csharpDocument.GeneratedHtml;
            }
        }

        internal record Directory(IReadOnlyList<Entry> Children);
    }


}
