﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public struct ClassifiedSpan
    {
        public ClassifiedSpan(SourceSpan span, SourceSpan blockSpan, SpanKind spanKind, BlockKind blockKind, AcceptedCharacters acceptedCharacters)
        {
            Span = span;
            BlockSpan = blockSpan;
            SpanKind = spanKind;
            BlockKind = blockKind;
            AcceptedCharacters = acceptedCharacters;
        }

        public AcceptedCharacters AcceptedCharacters { get; }

        public BlockKind BlockKind { get; }

        public SourceSpan BlockSpan { get; }

        public SourceSpan Span { get; }

        public SpanKind SpanKind { get; }
    }
}
