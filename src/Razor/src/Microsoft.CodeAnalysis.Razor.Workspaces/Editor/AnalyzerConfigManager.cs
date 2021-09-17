// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Editor
{
    internal abstract class AnalyzerConfigManager
    {
        /// <summary>
        /// Adds or updates the given analyzer config document in the set of analyzer configs.
        /// </summary>
        public abstract Task AddOrUpdateAnalyzerConfigDocumentAsync(
            DocumentId documentId,
            Solution newSolution,
            CancellationToken cancellationToken);

        /// <summary>
        /// Removes the given analyzer config document in the set of analyzer configs.
        /// </summary>
        public abstract Task RemoveAnalyzerConfigDocumentAsync(
            DocumentId documentId,
            Solution oldSolution,
            CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves the set of options which should apply to the given file path.
        /// </summary>
        public abstract Task<AnalyzerConfigOptionsResult?> GetOptionsForSourcePathAsync(
            string filePath,
            CancellationToken cancellationToken);
    }
}
