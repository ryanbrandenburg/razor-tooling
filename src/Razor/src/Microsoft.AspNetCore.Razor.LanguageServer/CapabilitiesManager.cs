// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

internal class CapabilitiesManager : IInitializeManager<InitializeParams, InitializeResult>
{
    private InitializeParams? _initializeParams;
    private readonly ILspServices _lspServices;

    public CapabilitiesManager(ILspServices lspServices)
    {
        _lspServices = lspServices;
    }

    public InitializeParams GetInitializeParams()
    {
        if (_initializeParams is null)
        {
            throw new InvalidOperationException($"{nameof(GetInitializeParams)} was called before '{Methods.InitializeName}'");
        }

        return _initializeParams;
    }

    public InitializeResult GetInitializeResult()
    {
        var initializeParams = GetInitializeParams();
        var clientCapabilities = initializeParams.Capabilities;

        var serverCapabilities = new VSInternalServerCapabilities();

        var registrationExtensions = _lspServices.GetRequiredServices<IRegistrationExtension>();
        foreach (var registrationExtension in registrationExtensions)
        {
            var registrationResult = registrationExtension.GetRegistration(clientCapabilities);
            if (registrationResult is not null)
            {
                serverCapabilities.ApplyRegistrationResult(registrationResult);
            }
        }

        var initializeResult = new InitializeResult
        {
            Capabilities = serverCapabilities,
        };

        return initializeResult;
    }

    public void SetInitializeParams(InitializeParams request)
    {
        _initializeParams = request;
    }
}
