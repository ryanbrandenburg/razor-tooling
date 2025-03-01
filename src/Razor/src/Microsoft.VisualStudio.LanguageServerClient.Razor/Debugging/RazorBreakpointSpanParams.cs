﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    internal class RazorBreakpointSpanParams
    {
        public Uri Uri { get; set; }

        public Position Position { get; set; }
    }
}
