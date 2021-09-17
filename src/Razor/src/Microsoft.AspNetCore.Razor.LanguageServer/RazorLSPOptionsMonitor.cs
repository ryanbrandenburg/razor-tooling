// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLSPOptionsMonitor
    {
        private readonly RazorConfigurationService _configurationService;
        private readonly MemoryCache<string, RazorLSPOptions> _filePathToOptionsCache = new();

        public RazorLSPOptionsMonitor(RazorConfigurationService configurationService)
        {
            if (configurationService is null)
            {
                throw new ArgumentNullException(nameof(configurationService));
            }

            _configurationService = configurationService;
        }

        public RazorLSPOptions GetCurrentOptions(string filePath)
        {
            if (_filePathToOptionsCache.TryGetValue(filePath, out var options))
            {
                return options;
            }

            // TO-DO: Recompute if options aren't found
            return RazorLSPOptions.Default;
        }

        public virtual async Task UpdateAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var latestOptions = await _configurationService.GetLatestOptionsAsync(filePath, cancellationToken);
            if (latestOptions is not null)
            {
                _filePathToOptionsCache.Set(filePath, latestOptions);
            }
        }
    }
}
