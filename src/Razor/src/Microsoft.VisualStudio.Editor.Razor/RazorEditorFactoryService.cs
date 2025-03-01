﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public abstract class RazorEditorFactoryService
    {
        public abstract bool TryGetDocumentTracker(ITextBuffer textBuffer, out VisualStudioDocumentTracker documentTracker);

        public abstract bool TryGetParser(ITextBuffer textBuffer, out VisualStudioRazorParser parser);

        internal abstract bool TryGetSmartIndenter(ITextBuffer textBuffer, out BraceSmartIndenter braceSmartIndenter);
    }
}
