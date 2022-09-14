﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert;
using Microsoft.AspNetCore.Razor.LanguageServer.Debugging;
using Microsoft.AspNetCore.Razor.LanguageServer.Definition;
using Microsoft.AspNetCore.Razor.LanguageServer.Diagnostics;
using Microsoft.AspNetCore.Razor.LanguageServer.DocumentColor;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.AspNetCore.Razor.LanguageServer.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Folding;
using Microsoft.AspNetCore.Razor.LanguageServer.LinkedEditingRange;
using Microsoft.AspNetCore.Razor.LanguageServer.Refactoring;
using Microsoft.AspNetCore.Razor.LanguageServer.WrapWithTag;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Editor.Razor;
using StreamJsonRpc;

namespace Microsoft.AspNetCore.Razor.LanguageServer;

internal class RazorLanguageServer : AbstractLanguageServer<RazorRequestContext>
{
    private readonly JsonRpc _jsonRpc;
    private readonly LanguageServerFeatureOptions? _featureOptions;
    private readonly ProjectSnapshotManagerDispatcher? _projectSnapshotManagerDispatcher;
    private readonly Action<IServiceCollection>? _configureServer;

    private readonly TaskCompletionSource<int> _tcs = new();

    public RazorLanguageServer(
        JsonRpc jsonRpc,
        ILspLogger logger,
        ProjectSnapshotManagerDispatcher? projectSnapshotManagerDispatcher,
        LanguageServerFeatureOptions? featureOptions,
        Action<IServiceCollection>? configureServer)
        : base(jsonRpc, logger)
    {
        _jsonRpc = jsonRpc;
        _featureOptions = featureOptions;
        _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
        _configureServer = configureServer;
    }

    protected override ILspServices ConstructLspServices()
    {
        var services = new ServiceCollection()
            .AddOptions()
            .AddLogging();

        if (_configureServer is not null)
        {
            _configureServer(services);
        }

        services.AddSingleton<ILspLogger>(RazorLspLogger.Instance);
        services.AddSingleton<ErrorReporter, LanguageServerErrorReporter>();

        if (_projectSnapshotManagerDispatcher is null)
        {
            services.AddSingleton<ProjectSnapshotManagerDispatcher, LSPProjectSnapshotManagerDispatcher>();
        }
        else
        {
            services.AddSingleton<ProjectSnapshotManagerDispatcher>(_projectSnapshotManagerDispatcher);
        }

        services.AddSingleton<AdhocWorkspaceFactory, DefaultAdhocWorkspaceFactory>();

        var featureOptions = _featureOptions ?? new DefaultLanguageServerFeatureOptions();
        services.AddSingleton(featureOptions);

        services.AddLifeCycleServices(this, _jsonRpc);

        services.AddSemanticTokensServices();
        services.AddDocumentManagmentServices();
        services.AddCompletionServices(featureOptions);
        services.AddFormattingServices();
        services.AddCodeActionsServices();
        services.AddOptionsServices();
        services.AddHoverServices();
        services.AddTextDocumentServices();

        // Auto insert
        services.AddSingleton<RazorOnAutoInsertProvider, CloseTextTagOnAutoInsertProvider>();
        services.AddSingleton<RazorOnAutoInsertProvider, AutoClosingTagOnAutoInsertProvider>();

        // Folding Range Providers
        services.AddSingleton<RazorFoldingRangeProvider, RazorCodeBlockFoldingProvider>();

        // Other
        services.AddSingleton<HtmlFactsService, DefaultHtmlFactsService>();
        services.AddSingleton<WorkspaceDirectoryPathResolver, DefaultWorkspaceDirectoryPathResolver>();
        services.AddSingleton<RazorComponentSearchEngine, DefaultRazorComponentSearchEngine>();

        // Folding Range Providers
        services.AddSingleton<RazorFoldingRangeProvider, RazorCodeBlockFoldingProvider>();

        AddHandlers(services);

        var lspServices = new LspServices(services);

        return lspServices;

        static void AddHandlers(IServiceCollection services)
        {
            services.AddHandler<RazorDiagnosticsEndpoint>();
            services.AddHandler<RazorConfigurationEndpoint>();
            services.AddRegisteringHandler<OnAutoInsertEndpoint>();
            services.AddHandler<MonitorProjectConfigurationFilePathEndpoint>();
            services.AddRegisteringHandler<RenameEndpoint>();
            services.AddRegisteringHandler<RazorDefinitionEndpoint>();
            services.AddRegisteringHandler<LinkedEditingRangeEndpoint>();
            services.AddHandler<WrapWithTagEndpoint>();
            services.AddHandler<RazorBreakpointSpanEndpoint>();
            services.AddHandler<RazorProximityExpressionsEndpoint>();
            services.AddRegisteringHandler<DocumentColorEndpoint>();
            services.AddRegisteringHandler<FoldingRangeEndpoint>();
        }
    }

    internal T GetRequiredService<T>() where T : notnull
    {
        var lspServices = GetLspServices();

        return lspServices.GetRequiredService<T>();
    }

    public override Task ExitAsync()
    {
        var exit = base.ExitAsync();

        _tcs.TrySetResult(0);
        return exit;
    }

    internal Task WaitForExit => _tcs.Task;
}
