// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    /**
     * A notification to signal an unsubscribe from a corresponding [watch](#watch) request.
     */
    internal class StopWatchingParams : IRequest
    {
        /**
         * The subscription id.
         */
        public string SubscriptionId { get; set; }
    }
}
