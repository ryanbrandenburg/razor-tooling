// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class ReadFileParams : IRequest<ReadFileResponse>
    {
        /**
         * The uri of the folder
         */
        public DocumentUri Uri { get; set; }
    }
}
