// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class WriteFileParams : IRequest
    {
        /**
         * The uri of the file to write
         */
        public Uri Uri { get; set; }

        /**
         * The new content of the file `base64` encoded.
         */
        public string Content { get; set; }

        /**
         * Options to define if missing files should or must be created.
         */
        public WriteFileOptions Options { get; set; }
    }
}
