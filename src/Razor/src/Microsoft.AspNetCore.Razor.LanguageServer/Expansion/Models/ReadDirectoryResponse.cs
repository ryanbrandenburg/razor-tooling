// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class ReadDirectoryResponse
    {
        /**
         * An array of nodes that represent the directories children.
         */
        public IReadOnlyList<DirectoryChild> Children { get; set; }
    }
}
