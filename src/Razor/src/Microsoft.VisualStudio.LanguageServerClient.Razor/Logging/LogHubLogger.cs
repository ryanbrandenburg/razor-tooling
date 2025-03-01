﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Logging
{
    internal class LogHubLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogHubLogWriter _logWriter;
        private readonly Scope _noopScope;

        public LogHubLogger(string categoryName, LogHubLogWriter feedbackFileLogWriter)
        {
            if (categoryName is null)
            {
                throw new ArgumentNullException(nameof(categoryName));
            }

            if (feedbackFileLogWriter is null)
            {
                throw new ArgumentNullException(nameof(feedbackFileLogWriter));
            }

            _categoryName = categoryName;
            _logWriter = feedbackFileLogWriter;
            _noopScope = new Scope();
        }

        public IDisposable BeginScope<TState>(TState state) => _noopScope;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            try
            {
                var formattedResult = formatter(state, exception);

                switch (logLevel)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                    case LogLevel.Information:
                    case LogLevel.None:
                        _logWriter.TraceInformation("[{0}] {1}", _categoryName, formattedResult);
                        break;
                    case LogLevel.Warning:
                        _logWriter.TraceWarning("[{0}] {1}", _categoryName, formattedResult);
                        break;
                    case LogLevel.Error:
                    case LogLevel.Critical:
                        _logWriter.TraceError("[{0}] {1} {2}", _categoryName, formattedResult, exception);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logWriter.TraceError("Error while trying to write log message: [{0}] {1}", _categoryName, ex);
            }
        }

        private class Scope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
