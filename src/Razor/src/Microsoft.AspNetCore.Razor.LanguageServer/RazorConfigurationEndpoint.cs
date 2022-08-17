﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorConfigurationEndpoint : IDidChangeConfigurationEndpoint
    {
        private readonly RazorLSPOptionsMonitor _optionsMonitor;

        public RazorConfigurationEndpoint(RazorLSPOptionsMonitor optionsMonitor)
        {
            if (optionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            _optionsMonitor = optionsMonitor;
        }

        public bool MutatesSolutionState => true;

        public object? GetTextDocumentIdentifier(DidChangeConfigurationParamsBridge request)
        {
            return null;
        }

        public async Task HandleNotificationAsync(DidChangeConfigurationParamsBridge request, RazorRequestContext context, CancellationToken cancellationToken)
        {
            context.Logger.LogInformation("Settings changed. Updating the server.");

            await _optionsMonitor.UpdateAsync(cancellationToken);
        }
    }
}
