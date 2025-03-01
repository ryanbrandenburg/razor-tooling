﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [Export(typeof(ErrorReporter))]
    internal class VisualStudioErrorReporter : ErrorReporter
    {
        private readonly SVsServiceProvider _services;

        [ImportingConstructor]
        public VisualStudioErrorReporter(SVsServiceProvider services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services;
        }

        public override void ReportError(Exception exception)
        {
            if (exception is null)
            {
                return;
            }

            var activityLog = GetActivityLog();
            if (activityLog != null)
            {
                var hr = activityLog.LogEntry(
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                    "Razor Language Services",
                    $"Error encountered:{Environment.NewLine}{exception}");
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        public override void ReportError(Exception exception, ProjectSnapshot project)
        {
            if (exception is null)
            {
                return;
            }

            var activityLog = GetActivityLog();
            if (activityLog != null)
            {
                var hr = activityLog.LogEntry(
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                    "Razor Language Services",
                    $"Error encountered from project '{project?.FilePath}':{Environment.NewLine}{exception}");
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        public override void ReportError(Exception exception, Project workspaceProject)
        {
            if (exception is null)
            {
                return;
            }

            var activityLog = GetActivityLog();
            if (activityLog != null)
            {
                var hr = activityLog.LogEntry(
                    (uint)__ACTIVITYLOG_ENTRYTYPE.ALE_ERROR,
                    "Razor Language Services",
                    $"Error encountered from project '{workspaceProject?.Name}' '{workspaceProject?.FilePath}':{Environment.NewLine}{exception}");
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        private IVsActivityLog GetActivityLog()
        {
            return _services.GetService(typeof(SVsActivityLog)) as IVsActivityLog;
        }
    }
}
