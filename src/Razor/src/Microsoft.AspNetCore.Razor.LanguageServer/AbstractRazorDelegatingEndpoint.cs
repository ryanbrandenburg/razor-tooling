﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Protocol;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal abstract class AbstractRazorDelegatingEndpoint<TRequest, TResponse> : IRazorRequestHandler<TRequest, TResponse>
        where TRequest : TextDocumentPositionParams
    {
        private readonly LanguageServerFeatureOptions _languageServerFeatureOptions;
        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly ClientNotifierServiceBase _languageServer;
        protected readonly ILogger Logger;

        protected AbstractRazorDelegatingEndpoint(
            LanguageServerFeatureOptions languageServerFeatureOptions,
            RazorDocumentMappingService documentMappingService,
            ClientNotifierServiceBase languageServer,
            ILogger logger)
        {
            _languageServerFeatureOptions = languageServerFeatureOptions ?? throw new ArgumentNullException(nameof(languageServerFeatureOptions));
            _documentMappingService = documentMappingService ?? throw new ArgumentNullException(nameof(documentMappingService));
            _languageServer = languageServer ?? throw new ArgumentNullException(nameof(languageServer));

            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// The delegated object to send to the <see cref="CustomMessageTarget"/>
        /// </summary>
        protected abstract IDelegatedParams CreateDelegatedParams(TRequest request, RazorRequestContext razorRequestContext, Projection projection, CancellationToken cancellationToken);

        /// <summary>
        /// The name of the endpoint to delegate to, from <see cref="RazorLanguageServerCustomMessageTargets"/>. This is the
        /// custom endpoint that is sent via <see cref="ClientNotifierServiceBase"/> which returns
        /// a response by delegating to C#/HTML.
        /// </summary>
        /// <remarks>
        /// An example is <see cref="RazorLanguageServerCustomMessageTargets.RazorHoverEndpointName"/>
        /// </remarks>
        protected abstract string CustomMessageTarget { get; }

        public abstract bool MutatesSolutionState { get; }

        /// <summary>
        /// If the response needs to be handled, such as for remapping positions back, override and handle here
        /// </summary>
        protected virtual Task<TResponse> HandleDelegatedResponseAsync(TResponse delegatedResponse, RazorRequestContext reqeuestContext, CancellationToken cancellationToken)
            => Task.FromResult(delegatedResponse);

        /// <summary>
        /// If the request can be handled without delegation, override this to provide a response. If a null
        /// value is returned the request will be delegated to C#/HTML servers, otherwise the response
        /// will be used in <see cref="HandleRequestAsync(TRequest, RazorRequestContext, CancellationToken)"/>
        /// </summary>
        protected virtual Task<TResponse?> TryHandleAsync(TRequest request, RazorRequestContext requestContext, Projection projection, CancellationToken cancellationToken)
            => Task.FromResult<TResponse?>(default);

        /// <summary>
        /// Returns true if the configuration supports this operation being handled, otherwise returns false. Use to
        /// handle cases where <see cref="LanguageServerFeatureOptions"/> other than <see cref="LanguageServerFeatureOptions.SingleServerSupport"/>
        /// need to be checked to validate that the operation can be done.
        /// </summary>
        protected virtual bool IsSupported() => true;

        /// <summary>
        /// Implementation for <see cref="HandleRequestAsync(TRequest, RazorRequestContext, CancellationToken)"/>
        /// </summary>
        public async Task<TResponse> HandleRequestAsync(TRequest request, RazorRequestContext context, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!IsSupported())
            {
                return default;
            }

            var projection = await _documentMappingService.TryGetProjectionAsync(documentContext, request.Position, Logger, cancellationToken).ConfigureAwait(false);
            if (projection is null)
            {
                return default;
            }

            context.RequireDocumentContext();
            var documentContext = context.DocumentContext;

            var projection = await _documentMappingService.TryGetProjectionAsync(documentContext, request.Position, context.Logger, cancellationToken).ConfigureAwait(false);
            if (projection is null)
            {
                return default;
            }

            var response = await TryHandleAsync(request, context, projection, cancellationToken).ConfigureAwait(false);
            if (response is not null)
            {
                return response;
            }

            if (!_languageServerFeatureOptions.SingleServerSupport)
            {
                return default;
            }

            var delegatedParams = CreateDelegatedParams(request, context, projection, cancellationToken);

            var delegatedRequest = await _languageServer.SendRequestAsync<TDelegatedParams, TResponse>(CustomMessageTarget, delegatedParams, cancellationToken).ConfigureAwait(false);

            var remappedResponse = await HandleDelegatedResponseAsync(delegatedRequest, context, cancellationToken).ConfigureAwait(false);

            return remappedResponse;
        }

        public object? GetTextDocumentIdentifier(TRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
