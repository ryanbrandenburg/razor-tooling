﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using Microsoft.Build.Execution;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public abstract class ProjectInstanceEvaluator
    {
        public abstract ProjectInstance Evaluate(ProjectInstance projectInstance);
    }
}
