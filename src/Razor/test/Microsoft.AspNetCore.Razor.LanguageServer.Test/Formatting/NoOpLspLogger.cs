// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class NoOpLspLogger : ILspLogger
    {
        public static NoOpLspLogger Instance = new NoOpLspLogger();

        public Task LogEndContextAsync(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogError(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogException(Exception exception, string? message = null, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogInformation(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogStartContext(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public Task LogWarning(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }
    }
}
