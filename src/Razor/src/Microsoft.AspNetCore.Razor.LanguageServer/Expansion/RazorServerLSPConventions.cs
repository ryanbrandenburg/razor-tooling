// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion
{
    internal static class RazorServerLSPConventions
    {
        public static bool IsVirtualCSharpFile(Uri uri) => CheckIfFileUriAndExtensionMatch(uri, RazorServerLSPConstants.VirtualCSharpFileNameSuffix);

        public static bool IsVirtualHtmlFile(Uri uri) => CheckIfFileUriAndExtensionMatch(uri, RazorServerLSPConstants.VirtualHtmlFileNameSuffix);

        public static bool IsRazorFile(Uri uri) => CheckIfFileUriAndExtensionMatch(uri, RazorServerLSPConstants.RazorFileExtension);

        public static bool IsCSHTMLFile(Uri uri) => CheckIfFileUriAndExtensionMatch(uri, RazorServerLSPConstants.CSHTMLFileExtension);

        public static Uri GetRazorDocumentUri(Uri virtualDocumentUri)
        {
            if (virtualDocumentUri is null)
            {
                throw new ArgumentNullException(nameof(virtualDocumentUri));
            }

            var path = virtualDocumentUri.AbsoluteUri;
            path = path.Replace(RazorServerLSPConstants.VirtualCSharpFileNameSuffix, string.Empty);
            path = path.Replace(RazorServerLSPConstants.VirtualHtmlFileNameSuffix, string.Empty);

            var uri = new Uri(path, UriKind.Absolute);
            return uri;
        }

        private static bool CheckIfFileUriAndExtensionMatch(Uri uri, string extension)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (string.IsNullOrEmpty(extension))
            {
                throw new ArgumentNullException(nameof(extension));
            }

            return uri.GetAbsoluteOrUNCPath()?.EndsWith(extension, StringComparison.Ordinal) ?? false;
        }
    }
}
