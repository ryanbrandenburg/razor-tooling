// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal abstract class GeneratedDocumentPublisher : ProjectSnapshotChangeTrigger
    {
        public abstract Task PublishGeneratedDocumentsAsync(string filePath, RazorCodeDocument codeDocument, int hostDocumentVersion, CancellationToken cancellationToken);

        public abstract void PublishCSharp(string filePath, SourceText sourceText, int hostDocumentVersion);

        public abstract void PublishHtml(string filePath, SourceText sourceText, int hostDocumentVersion);
    }
}
