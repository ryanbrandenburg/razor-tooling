// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.Workspaces
{
    internal class RazorOpenDocumentService : IRazorDocumentOpenService
    {
        private const string RazorGeneratedDocumentExtension = ".g.cs";

        private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
        private readonly ProjectSnapshotManager _projectSnapshotManager;

        public RazorOpenDocumentService(ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher, ProjectSnapshotManager projectSnapshotManager)
        {
            if (projectSnapshotManagerDispatcher is null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerDispatcher));
            }

            if (projectSnapshotManager is null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManager));
            }

            _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
            _projectSnapshotManager = projectSnapshotManager;
        }

        public async Task<bool> IsDocumentOpenAsync(Document generatedDocument, CancellationToken cancellationToken)
        {
            if (!TryGetRazorDocumentPath(generatedDocument, out var razorDocumentPath))
            {
                return false;
            }

            var isOpen = await _projectSnapshotManagerDispatcher.RunOnDispatcherThreadAsync(
                () => _projectSnapshotManager.IsDocumentOpen(razorDocumentPath), cancellationToken).ConfigureAwait(false);
            return isOpen;
        }

        private static bool TryGetRazorDocumentPath(Document generatedDocument, out string? razorDocumentPath)
        {
            if (generatedDocument.FilePath is null || !generatedDocument.FilePath.EndsWith(RazorGeneratedDocumentExtension))
            {
                razorDocumentPath = null;
                return false;
            }

            razorDocumentPath = generatedDocument.FilePath.Substring(
                0, generatedDocument.FilePath.Length - RazorGeneratedDocumentExtension.Length);
            return true;

        }
    }
}
