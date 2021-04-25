// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.LanguageServer.Expansion.EndpointContracts;
using Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion
{
    internal class RazorFileSystemEndpoint : IFileSystemHandler, IRegistrationExtension
    {
        private readonly FileSystemProvider _fileSystemProvider;

        public RazorFileSystemEndpoint(FileSystemProvider fileSystemProvider)
        {
            if (fileSystemProvider is null)
            {
                throw new ArgumentNullException(nameof(fileSystemProvider));
            }

            _fileSystemProvider = fileSystemProvider;
        }

        public RegistrationExtensionResult GetRegistration()
        {
            var options = new FileSystemProviderOptions()
            {
                Scheme = RazorServerLSPConstants.EmbeddedFileScheme,
                IsCaseSensitive = true,
                IsReadonly = true,
            };
            var registrationResult = new RegistrationExtensionResult("fileSystem", options);
            return registrationResult;
        }

        public async Task<ReadFileResponse> Handle(ReadFileParams request, CancellationToken cancellationToken)
        {
            var uri = request.Uri.ToUri();
            var content = await _fileSystemProvider.ReadFileAsync(uri, cancellationToken);
            var bytes = Encoding.UTF8.GetBytes(content);
            var base64Content = Convert.ToBase64String(bytes);
            var response = new ReadFileResponse()
            {
                Content = base64Content,
            };

            return response;
        }

        public Task<Unit> Handle(WatchParams request, CancellationToken cancellationToken)
        {
            _fileSystemProvider.Watch(request.Uri, request.SubscriptionId, request.Options);
            return Unit.Task;
        }

        public Task<Unit> Handle(StopWatchingParams request, CancellationToken cancellationToken)
        {
            _fileSystemProvider.StopWatching(request.SubscriptionId);
            return Unit.Task;
        }

        public async Task<FileStatResponse> Handle(FileStatParams request, CancellationToken cancellationToken)
        {
            var uri = request.Uri.ToUri();
            var fileStat = await _fileSystemProvider.StatAsync(uri, cancellationToken).ConfigureAwait(false);
            var response = new FileStatResponse(fileStat);
            return response;
        }

        public async Task<ReadDirectoryResponse> Handle(ReadDirectoryParams request, CancellationToken cancellationToken)
        {
            var children = await _fileSystemProvider.ReadDirectoryAsync(request.Uri, cancellationToken);
            var response = new ReadDirectoryResponse()
            {
                Children = children,
            };
            return response;
        }

        public async Task<Unit> Handle(CreateDirectoryParams request, CancellationToken cancellationToken)
        {
            await _fileSystemProvider.CreateDirectoryAsync(request.Uri, cancellationToken).ConfigureAwait(false);

            return Unit.Value;
        }

        public async Task<Unit> Handle(WriteFileParams request, CancellationToken cancellationToken)
        {
            var base64Content = request.Content;
            var data = Convert.FromBase64String(base64Content);
            var content = Encoding.UTF8.GetString(data);
            await _fileSystemProvider.WriteFileAsync(request.Uri, content, request.Options, cancellationToken).ConfigureAwait(false);
            return Unit.Value;
        }

        public async Task<Unit> Handle(DeleteFileParams request, CancellationToken cancellationToken)
        {
            await _fileSystemProvider.DeleteAsync(request.Uri, request.Options, cancellationToken);
            return Unit.Value;
        }

        public async Task<Unit> Handle(RenameFileParams request, CancellationToken cancellationToken)
        {
            await _fileSystemProvider.RenameAsync(request.OldUri, request.NewUri, request.Options, cancellationToken);
            return Unit.Value;
        }
    }
}
