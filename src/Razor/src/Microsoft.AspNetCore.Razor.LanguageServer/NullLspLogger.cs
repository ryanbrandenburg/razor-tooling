// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class NullLspLogger : ILspLogger
    {
        public static NullLspLogger Instance = new NullLspLogger();

        public Task LogEndContextAsync(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogErrorAsync(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogExceptionAsync(Exception exception, string? message = null, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogInformationAsync(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogStartContextAsync(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogWarningAsync(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }
    }
}
