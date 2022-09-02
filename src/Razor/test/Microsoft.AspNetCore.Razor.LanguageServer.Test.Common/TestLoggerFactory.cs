// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Razor.Test.Common
{
    public class TestLspLogger : ILspLogger
    {
        public static readonly TestLspLogger Instance = new();

        public void LogEndContext(string message, params object[] @params)
        {
        }

        public void LogError(string message, params object[] @params)
        {
        }

        public void LogException(Exception exception, string message = null, params object[] @params)
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

    public class TestLogger : ILogger
    {
        public static TestLogger Instance = new TestLogger();
        public IDisposable BeginScope<TState>(TState state)
        {
            return new Disposable();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        private class Disposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    public class TestLoggerFactory : ILoggerFactory
    {
        public static readonly TestLoggerFactory Instance = new();

        private TestLoggerFactory()
        {

        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => new TestLogger();

        public void Dispose()
        {
        }

        private class TestLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => new DisposableScope();

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
            }

            private class DisposableScope : IDisposable
            {
                public void Dispose()
                {
                }
            }
        }
    }
}
