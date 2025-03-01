﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public abstract class LSPDocument : IDisposable
    {
        public abstract int Version { get; }

        public abstract Uri Uri { get; }

        public abstract ITextBuffer TextBuffer { get; }

        public abstract LSPDocumentSnapshot CurrentSnapshot { get; }

        public abstract IReadOnlyList<VirtualDocument> VirtualDocuments { get; }

        public abstract LSPDocumentSnapshot UpdateVirtualDocument<TVirtualDocument>(IReadOnlyList<ITextChange> changes, int hostDocumentVersion, object state) where TVirtualDocument : VirtualDocument;

        public bool TryGetVirtualDocument<TVirtualDocument>(out TVirtualDocument virtualDocument) where TVirtualDocument : VirtualDocument
        {
            for (var i = 0; i < VirtualDocuments.Count; i++)
            {
                if (VirtualDocuments[i] is TVirtualDocument actualVirtualDocument)
                {
                    virtualDocument = actualVirtualDocument;
                    return true;
                }
            }

            virtualDocument = null;
            return false;
        }

        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/4801")]
        public virtual void Dispose()
        {
            foreach (var virtualDocument in VirtualDocuments)
            {
                virtualDocument.Dispose();
            }
        }
    }
}
