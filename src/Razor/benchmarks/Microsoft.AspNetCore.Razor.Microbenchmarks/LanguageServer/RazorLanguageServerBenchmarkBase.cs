// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.AspNetCore.Razor.LanguageServer.RazorLanguageServerTarget;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.LanguageServer
{
    public class RazorLanguageServerBenchmarkBase : ProjectSnapshotManagerBenchmarkBase
    {
        public RazorLanguageServerBenchmarkBase()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "src", "Razor", "Razor.sln")))
            {
                current = current.Parent;
            }

            RepoRoot = current.FullName;

            using var memoryStream = new MemoryStream();
           // RazorLanguageServerTask = RazorLanguageServer.CreateAsync(memoryStream, memoryStream, Trace.Off, razorLspServiceProvider: null, asynchronousOperationListenerProvider: null);
        }

        protected internal virtual void Builder(RazorLanguageServerBuilder builder)
        {
        }

        protected string RepoRoot { get; }

        private protected Task<RazorLanguageServer> RazorLanguageServerTask { get; }

        internal DocumentSnapshot GetDocumentSnapshot(string projectFilePath, string filePath, string targetPath)
        {
            var hostProject = new HostProject(projectFilePath, RazorConfiguration.Default, rootNamespace: null);
            using var fileStream = new FileStream(filePath, FileMode.Open);
            var text = SourceText.From(fileStream);
            var textLoader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()));
            var hostDocument = new HostDocument(filePath, targetPath, FileKinds.Component);

            var projectSnapshotManager = CreateProjectSnapshotManager();
            projectSnapshotManager.ProjectAdded(hostProject);
            projectSnapshotManager.DocumentAdded(hostProject, hostDocument, textLoader);
            var projectSnapshot = projectSnapshotManager.GetOrCreateProject(projectFilePath);

            var documentSnapshot = projectSnapshot.GetDocument(filePath);
            return documentSnapshot;
        }

        private class NoopClientNotifierService : ClientNotifierServiceBase
        {
            public override Task OnStartedAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public override Task SendNotificationAsync<TParams>(string method, TParams @params, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task SendNotificationAsync(string method, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override Task<TResponse> SendRequestAsync<TParams, TResponse>(string method, TParams @params, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
