// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class RenameFileParams : IRequest
    {
        /**
         * The existing file.
         */
        public Uri OldUri { get; set; }

        /**
         * The new location.
         */
        public Uri NewUri { get; set; }

        /**
         * Defines if existing files should be overwritten.
         */
        public RenameFileOptions Options { get; set; }
    }
}
