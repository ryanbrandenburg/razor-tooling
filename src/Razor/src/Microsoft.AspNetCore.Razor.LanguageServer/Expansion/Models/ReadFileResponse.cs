// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class ReadFileResponse
    {
        /**
         * The entire contents of the file `base64` encoded.
         */
        public string Content { get; set; }
    }
}
