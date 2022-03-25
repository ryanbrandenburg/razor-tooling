// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using static Microsoft.VisualStudio.Editor.Razor.TagHelperFactsService;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public sealed class ElementCompletionContext
    {
        internal ElementCompletionContext(
            TagHelperDocumentContext documentContext!!,
            IEnumerable<string> existingCompletions!!,
            string containingTagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            Func<string, bool> inHTMLSchema!!,
            IEnumerable<AncestorInfo> ancestors!!)
        {
            DocumentContext = documentContext;
            ExistingCompletions = existingCompletions;
            ContainingTagName = containingTagName;
            Attributes = attributes;
            InHTMLSchema = inHTMLSchema;
            Ancestors = ancestors;
        }

        public TagHelperDocumentContext DocumentContext { get; }

        public IEnumerable<string> ExistingCompletions { get; }

        public string ContainingTagName { get; }

        public IEnumerable<KeyValuePair<string, string>> Attributes { get; }

        public Func<string, bool> InHTMLSchema { get; }

        internal IEnumerable<AncestorInfo> Ancestors { get; }

        public string? ParentTagName => Parent?.AncestorTagName;

        public string? GrandParentTagName => GrandParent?.AncestorTagName;

        public bool? GrandParentIsTagHelper => GrandParent?.AncestorIsTagHelper;

        private AncestorInfo? Parent => Ancestors.FirstOrDefault();

        private AncestorInfo? GrandParent => Ancestors.Count() > 1 ? Ancestors.ElementAt(1) : null;
    }
}
