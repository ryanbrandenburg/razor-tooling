// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class NoOpLspLogger : ILspLogger
    {
        public static NoOpLspLogger Instance = new NoOpLspLogger();

        public void LogEndContext(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public void LogError(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public void LogException(Exception exception, string? message = null, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public void LogInformation(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public void LogStartContext(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }

        public void LogWarning(string message, params object[] @params)
        {
            throw new NotImplementedException();
        }
    }
}
