// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using Microsoft.VisualStudio.LanguageServer.Client;

#nullable enable

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    internal struct ReinvokeResponse<TOut>
    {
        public ILanguageClient? LanguageClient { get; }

        public string LanguageClientName { get; }

        public TOut Result { get; }

        public bool IsSuccess => LanguageClientName != default;

        public ReinvokeResponse(ILanguageClient languageClient, TOut result)
        {
            LanguageClient = languageClient;
            LanguageClientName = languageClient.Name;
            Result = result;
        }

        public ReinvokeResponse(string languageClientName, TOut result)
        {
            LanguageClient = null;
            LanguageClientName = languageClientName;
            Result = result;
        }
    }
}
