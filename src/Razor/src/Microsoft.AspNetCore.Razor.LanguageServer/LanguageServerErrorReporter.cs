// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class LanguageServerErrorReporter : ErrorReporter
    {
        private readonly ILspLogger _logger;

        public LanguageServerErrorReporter(ILspLogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
        }

        public override void ReportError(Exception exception)
        {
            _ = _logger.LogExceptionAsync(exception, "Error thrown from LanguageServer");
        }

        public override void ReportError(Exception exception, ProjectSnapshot? project)
        {
            _ = _logger.LogExceptionAsync(exception, "Error thrown from project {projectFilePath}", project?.FilePath ?? "null");
        }

        public override void ReportError(Exception exception, Project workspaceProject)
        {
            _ = _logger.LogExceptionAsync(exception, "Error thrown from project {workspaceProjectFilePath}", workspaceProject.FilePath ?? "null");
        }
    }
}
