﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectSnapshotManager : ILanguageService
    {
        public abstract event EventHandler<ProjectChangeEventArgs> Changed;

        public abstract IReadOnlyList<ProjectSnapshot> Projects { get; }

        public abstract bool IsDocumentOpen(string documentFilePath);

        public abstract ProjectSnapshot GetLoadedProject(string filePath);

        public abstract ProjectSnapshot GetOrCreateProject(string filePath);
    }
}
