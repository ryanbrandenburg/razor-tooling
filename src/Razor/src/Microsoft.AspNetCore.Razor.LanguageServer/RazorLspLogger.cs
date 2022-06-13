// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLspLogger : ILspLogger
    {
        public static RazorLspLogger Instance = new RazorLspLogger();

        public Task LogEndContextAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogErrorAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogExceptionAsync(Exception exception, string? message = null, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogInformationAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogStartContextAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }

        public Task LogWarningAsync(string message, params object[] @params)
        {
            return Task.CompletedTask;
        }
    }
}
