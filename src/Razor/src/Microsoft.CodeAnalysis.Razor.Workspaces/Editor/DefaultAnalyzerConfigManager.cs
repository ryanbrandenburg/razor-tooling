// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Editor
{
    [Shared]
    [Export(typeof(AnalyzerConfigManager))]
    internal class DefaultAnalyzerConfigManager : AnalyzerConfigManager
    {
        /// <summary>
        /// The current set of analyzer config document IDs.
        /// </summary>
        private readonly HashSet<DocumentId> _analyzerConfigDocumentIds = new();

        /// <summary>
        /// Lock restricting access to _analyzerConfigDocumentIds and _currentAnalyzerConfigSet.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new(1);

        /// <summary>
        /// The current set of parsed analyzer configs.
        /// </summary>
        private AnalyzerConfigSet? _currentAnalyzerConfigSet;

        public override async Task AddOrUpdateAnalyzerConfigDocumentAsync(
            DocumentId documentId,
            Solution newSolution,
            CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var configDocument = newSolution.GetAnalyzerConfigDocument(documentId);
                Assumes.NotNull(configDocument);

                _analyzerConfigDocumentIds.Add(documentId);

                // Re-parse the current set of analyer configs to account for the added/updated document.
                await ParseAndSetOptionsAsync(newSolution, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task RemoveAnalyzerConfigDocumentAsync(
            DocumentId documentId,
            Solution oldSolution,
            CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var configDocument = oldSolution.GetAnalyzerConfigDocument(documentId);
                Assumes.NotNull(configDocument);

                _analyzerConfigDocumentIds.Remove(documentId);

                // Re-parse the current set of analyer configs to account for the removed document.
                await ParseAndSetOptionsAsync(oldSolution, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override AnalyzerConfigSet? GetCurrentAnalyzerConfigSet() => _currentAnalyzerConfigSet;

        private async Task ParseAndSetOptionsAsync(Solution solution, CancellationToken cancellationToken)
        {
            var analyzerConfigs = new HashSet<AnalyzerConfig>();

            // Retrieve each analyzer config document, parse it, and add the parsed config to the set of
            // analyzer configs.
            foreach (var documentId in _analyzerConfigDocumentIds)
            {
                var configDocument = solution.GetAnalyzerConfigDocument(documentId);
                Assumes.NotNull(configDocument);

                var text = await configDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var analyzerConfig = AnalyzerConfig.Parse(text, configDocument.FilePath);
                analyzerConfigs.Add(analyzerConfig);
            }

            var configSet = AnalyzerConfigSet.Create(analyzerConfigs);
            _currentAnalyzerConfigSet = configSet;
        }
    }
}
