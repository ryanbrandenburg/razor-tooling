﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public abstract class VisualStudioDocumentTracker
    {
        public abstract event EventHandler<ContextChangeEventArgs> ContextChanged;

        public abstract RazorConfiguration Configuration { get; }

        public abstract EditorSettings EditorSettings { get; }

        public abstract IReadOnlyList<TagHelperDescriptor> TagHelpers { get; }

        public abstract bool IsSupportedProject { get; }

        public abstract string FilePath { get; }

        public abstract string ProjectPath { get; }

        internal abstract ProjectSnapshot ProjectSnapshot { get; }

        public abstract Workspace Workspace { get; }

        public abstract ITextBuffer TextBuffer { get; }

        public abstract IReadOnlyList<ITextView> TextViews { get; }

        public abstract ITextView GetFocusedTextView();
    }
}
