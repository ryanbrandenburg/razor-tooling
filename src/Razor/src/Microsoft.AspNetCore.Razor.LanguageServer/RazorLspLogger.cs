// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLspLogger : ILspLogger
    {
        public static RazorLspLogger Instance = new RazorLspLogger();

        public void LogEndContext(string message, params object[] @params)
        {
        }

        public void LogError(string message, params object[] @params)
        {
        }

        public void LogException(Exception exception, string? message = null, params object[] @params)
        {
        }

        public void LogInformation(string message, params object[] @params)
        {
        }

        public void LogStartContext(string message, params object[] @params)
        {
        }

        public void LogWarning(string message, params object[] @params)
        {
        }
    }
}
