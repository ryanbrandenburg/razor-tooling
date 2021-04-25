// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using MediatR;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Expansion.Models
{
    internal class WatchParams : IRequest
    {
        /**
         * The uri of the file or folder to be watched.
         */
        public Uri Uri { get; set; }

        /**
         * The subscription ID to be used in order to stop watching the provided file or folder uri via the [StopWatching](#stopWatching) notification.
         */
        public string SubscriptionId { get; set; }

        /**
         * Configures the watch
         */
        public WatchFileOptions Options { get; set; }
    }
}
