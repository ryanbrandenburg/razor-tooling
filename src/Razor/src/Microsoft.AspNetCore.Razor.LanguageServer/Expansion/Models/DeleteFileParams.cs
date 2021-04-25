// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class DeleteFileParams : IRequest
    {
        /**
         * The uri of the file or folder to delete
         */
        public Uri Uri { get; set; }

        /**
         * Defines if deletion of folders is recursive.
         */
        public DeleteFileOptions Options { get; set; }
    }
}
