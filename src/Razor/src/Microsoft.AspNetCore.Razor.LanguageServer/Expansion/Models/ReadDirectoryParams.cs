// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class ReadDirectoryParams : IRequest<ReadDirectoryResponse>
    {
        /**
         * The uri of the folder.
         */
        public Uri Uri { get; set; }
    }
}
