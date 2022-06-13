// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    //internal class DefaultlanguageServerSettingsManager : ILanguageServerSettingsManager
    //{
    //    private readonly TaskCompletionSource<int> _taskCompletionSource = new();

    //    private InitializeParams? _initializeParams;

    //    public void SetInitializeParams(InitializeParams initializeParams)
    //    {
    //        _initializeParams = initializeParams;
    //        _taskCompletionSource.SetResult(0);
    //    }

    //    public InitializeParams GetInitializeParams()
    //    {
    //        if (_initializeParams is null)
    //        {
    //            throw new InvalidOperationException($"{nameof(GetInitializeParams)} called before {Methods.InitializeName}");
    //        }

    //        return _initializeParams;
    //    }

    //    public Task WaitForInitializedAsync(CancellationToken cancellationToken)
    //    {
    //        return _taskCompletionSource.Task;
    //    }
    //}

    //internal interface ILanguageServerSettingsManager
    //{
    //    public InitializeParams GetInitializeParams();

    //    public void SetInitializeParams(InitializeParams initializeParams);

    //    public Task WaitForInitializedAsync(CancellationToken cancellationToken);
    //}
}
