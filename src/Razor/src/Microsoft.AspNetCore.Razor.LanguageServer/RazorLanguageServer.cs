// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using CommonLanguageServerProtocol.Framework.Handlers;
using Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Debugging;
using Microsoft.AspNetCore.Razor.LanguageServer.Definition;
using Microsoft.AspNetCore.Razor.LanguageServer.Diagnostics;
using Microsoft.AspNetCore.Razor.LanguageServer.DocumentColor;
using Microsoft.AspNetCore.Razor.LanguageServer.DocumentPresentation;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.AspNetCore.Razor.LanguageServer.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Folding;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.AspNetCore.Razor.LanguageServer.Hover;
using Microsoft.AspNetCore.Razor.LanguageServer.LinkedEditingRange;
using Microsoft.AspNetCore.Razor.LanguageServer.Refactoring;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.AspNetCore.Razor.LanguageServer.WrapWithTag;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorLanguageServerTarget : AbstractLanguageServer<RazorRequestContext>
    {
        private readonly StreamJsonRpc.JsonRpc _jsonRpc;
        private readonly ProjectSnapshotManagerDispatcher _projectSnapshotManagerDispatcher;
        private readonly LanguageServerFeatureOptions _featureOptions;
        private readonly Action<IServiceCollection> _configureServer;

        public RazorLanguageServerTarget(
            StreamJsonRpc.JsonRpc jsonRpc,
            ILspLogger logger,
            ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher,
            LanguageServerFeatureOptions featureOptions,
            Action<IServiceCollection> configureServer)
            : base(jsonRpc, logger)
        {
            _jsonRpc = jsonRpc;
            _projectSnapshotManagerDispatcher = projectSnapshotManagerDispatcher;
            _featureOptions = featureOptions;
            _configureServer = configureServer;
        }

        protected override ILspServices ConstructLspServices()
        {
            var services = new ServiceCollection()
                .AddOptions()
                .AddLogging();

            _configureServer(services);
            var lifeCycleManager = new LifeCycleManager<RazorRequestContext>(this);
            services.AddSingleton<LifeCycleManager<RazorRequestContext>>(lifeCycleManager);
            services.AddSingleton<IInitializeManager<InitializeParams, InitializeResult>, CapabilitiesManager>();
            services.AddSingleton<IRequestContextFactory<RazorRequestContext>, RazorRequestContextFactory>();

            var serverManager = new DefaultClientNotifierService(_jsonRpc);
            services.AddSingleton<ClientNotifierServiceBase>(serverManager);
            services.AddSingleton<IOnLanguageServerStarted>(serverManager);
            services.AddSingleton<ILspLogger>(RazorLspLogger.Instance);
            services.AddSingleton<ProjectSnapshotManagerDispatcher>(_projectSnapshotManagerDispatcher);

            services.AddSingleton<ErrorReporter, LanguageServerErrorReporter>();

            services.AddSingleton<DocumentContextFactory, DefaultDocumentContextFactory>();
            services.AddSingleton<FilePathNormalizer>();
            services.AddSingleton<ProjectSnapshotManagerDispatcher, LSPProjectSnapshotManagerDispatcher>();
            services.AddSingleton<GeneratedDocumentPublisher, DefaultGeneratedDocumentPublisher>();
            services.AddSingleton<AdhocWorkspaceFactory, DefaultAdhocWorkspaceFactory>();
            services.AddSingleton<ProjectSnapshotChangeTrigger>((services) => services.GetRequiredService<GeneratedDocumentPublisher>());

            services.AddSingleton<WorkspaceSemanticTokensRefreshPublisher, DefaultWorkspaceSemanticTokensRefreshPublisher>();
            services.AddSingleton<ProjectSnapshotChangeTrigger, DefaultWorkspaceSemanticTokensRefreshTrigger>();

            services.AddDocumentManagmentServices();

            // Completion
            services.AddCompletionServices();

            // Auto insert
            services.AddSingleton<RazorOnAutoInsertProvider, CloseTextTagOnAutoInsertProvider>();
            services.AddSingleton<RazorOnAutoInsertProvider, AutoClosingTagOnAutoInsertProvider>();

            // Folding Range Providers
            services.AddSingleton<RazorFoldingRangeProvider, RazorCodeBlockFoldingProvider>();

            services.AddFormattingServices();

            // CSharp Code actions
            services.AddSingleton<CSharpCodeActionProvider, TypeAccessibilityCodeActionProvider>();
            services.AddSingleton<CSharpCodeActionProvider, DefaultCSharpCodeActionProvider>();
            services.AddSingleton<CSharpCodeActionResolver, DefaultCSharpCodeActionResolver>();
            services.AddSingleton<CSharpCodeActionResolver, AddUsingsCSharpCodeActionResolver>();
            services.AddSingleton<CSharpCodeActionResolver, UnformattedRemappingCSharpCodeActionResolver>();

            // Other
            services.AddSingleton<RazorSemanticTokensInfoService, DefaultRazorSemanticTokensInfoService>();
            services.AddSingleton<RazorHoverInfoService, DefaultRazorHoverInfoService>();
            services.AddSingleton<HtmlFactsService, DefaultHtmlFactsService>();
            services.AddSingleton<WorkspaceDirectoryPathResolver, DefaultWorkspaceDirectoryPathResolver>();
            services.AddSingleton<RazorComponentSearchEngine, DefaultRazorComponentSearchEngine>();

            // Options
            services.AddSingleton<RazorConfigurationService, DefaultRazorConfigurationService>();
            services.AddSingleton<RazorLSPOptionsMonitor>();
            services.AddSingleton<IOptionsMonitor<RazorLSPOptions>, RazorLSPOptionsMonitor>();

            // Auto insert
            services.AddSingleton<RazorOnAutoInsertProvider, CloseTextTagOnAutoInsertProvider>();
            services.AddSingleton<RazorOnAutoInsertProvider, AutoClosingTagOnAutoInsertProvider>();

            // Folding Range Providers
            services.AddSingleton<RazorFoldingRangeProvider, RazorCodeBlockFoldingProvider>();

            // Razor Code actions
            services.AddSingleton<RazorCodeActionProvider, ExtractToCodeBehindCodeActionProvider>();
            services.AddSingleton<RazorCodeActionResolver, ExtractToCodeBehindCodeActionResolver>();
            services.AddSingleton<RazorCodeActionProvider, ComponentAccessibilityCodeActionProvider>();
            services.AddSingleton<RazorCodeActionResolver, CreateComponentCodeActionResolver>();
            services.AddSingleton<RazorCodeActionResolver, AddUsingsCodeActionResolver>();

            // Defaults: For when the caller hasn't provided them through the `configure` action.
            //services.TryAddSingleton<HostServicesProvider, DefaultHostServicesProvider>();

            AddHandlers(services, _featureOptions);

            var lspServices = new LspServices(services);

            return lspServices;

            void AddHandlers(IServiceCollection services, LanguageServerFeatureOptions featureOptions)
            {
                // Lifetime Endpoints
                AddHandler<RazorInitializeEndpoint>(services);
                AddHandler<RazorInitializedEndpoint>(services);
                AddHandler<ExitHandler<RazorRequestContext>>(services);
                AddHandler<ShutdownHandler<RazorRequestContext>>(services);

                // TextDocument sync Endpoints
                AddRegisteringHandler<RazorDidChangeTextDocumentEndpoint>(services);
                AddHandler<RazorDidCloseTextDocumentEndpoint>(services);
                AddHandler<RazorDidOpenTextDocumentEndpoint>(services);

                // Format Endpoints
                AddRegisteringHandler<RazorDocumentFormattingEndpoint>(services);
                AddRegisteringHandler<RazorDocumentOnTypeFormattingEndpoint>(services);
                AddRegisteringHandler<RazorDocumentRangeFormattingEndpoint>(services);

                // CodeAction Endpoints
                AddRegisteringHandler<CodeActionEndpoint>(services);
                AddHandler<CodeActionResolutionEndpoint>(services);

                AddRegisteringHandler<RazorHoverEndpoint>(services);
                AddHandler<RazorMapToDocumentRangesEndpoint>(services);
                AddHandler<RazorDiagnosticsEndpoint>(services);
                AddHandler<RazorConfigurationEndpoint>(services);
                AddRegisteringHandler<RazorSemanticTokensEndpoint>(services);
                AddRegisteringHandler<SemanticTokensRefreshEndpoint>(services);
                AddRegisteringHandler<OnAutoInsertEndpoint>(services);
                AddHandler<MonitorProjectConfigurationFilePathEndpoint>(services);
                AddRegisteringHandler<RenameEndpoint>(services);
                AddRegisteringHandler<RazorDefinitionEndpoint>(services);
                AddRegisteringHandler<LinkedEditingRangeEndpoint>(services);
                AddHandler<WrapWithTagEndpoint>(services);
                AddRegisteringHandler<InlineCompletionEndpoint>(services);
                AddHandler<RazorBreakpointSpanEndpoint>(services);
                AddHandler<RazorProximityExpressionsEndpoint>(services);
                AddRegisteringHandler<DocumentColorEndpoint>(services);
                AddRegisteringHandler<FoldingRangeEndpoint>(services);
                AddRegisteringHandler<TextDocumentTextPresentationEndpoint>(services);
                AddRegisteringHandler<TextDocumentUriPresentationEndpoint>(services);

                services.AddSingleton(featureOptions);

                if (featureOptions.SingleServerCompletionSupport)
                {
                    AddRegisteringHandler<RazorCompletionEndpoint>(services);
                    AddHandler<RazorCompletionResolveEndpoint>(services);
                }
                else
                {
                    AddRegisteringHandler<LegacyRazorCompletionEndpoint>(services);
                    AddHandler<LegacyRazorCompletionResolveEndpoint>(services);
                }

                void AddRegisteringHandler<T>(IServiceCollection services) where T : class, IMethodHandler, IRegistrationExtension
                {
                    services.AddSingleton<T>();
                    services.AddSingleton<IMethodHandler, T>(s => s.GetRequiredService<T>());
                    // Transient because it should only be used once and I'm hoping it doesn't stick around.
                    services.AddTransient<IRegistrationExtension, T>(s => s.GetRequiredService<T>());
                }

                void AddHandler<T>(IServiceCollection services) where T : class, IMethodHandler
                {
                    if (typeof(IRegistrationExtension).IsAssignableFrom(typeof(T)))
                    {
                        throw new NotImplementedException($"{nameof(T)} is not using {nameof(AddRegisteringHandler)} when it implements {nameof(IRegistrationExtension)}");
                    }

                    services.AddSingleton<IMethodHandler, T>();
                }
            }
        }

        internal T GetRequiredService<T>() where T : notnull
        {
            var lspServices = GetLspServices();

            return lspServices.GetRequiredService<T>();
        }

        internal IObservable<bool> Shutdown;

        internal sealed class RazorLanguageServer : IAsyncDisposable
        {
            private readonly RazorLanguageServerTarget _innerServer;
            private readonly object _disposeLock;
            private bool _disposed;

            private RazorLanguageServer(RazorLanguageServerTarget innerServer)
            {
                if (innerServer is null)
                {
                    throw new ArgumentNullException(nameof(innerServer));
                }

                _innerServer = innerServer;
                _disposeLock = new object();
            }

            public static async Task<RazorLanguageServer> CreateAsync(
                Stream input,
                Stream output,
                Trace trace,
                ProjectSnapshotManagerDispatcher projectSnapshotManagerDispatcher,
                Action<IServiceCollection> configure,
                LanguageServerFeatureOptions featureOptions)
            {

                var logLevel = RazorLSPOptions.GetLogLevelForTrace(trace);
                var logger = new RazorLspLogger();
                var jsonRpc = CreateJsonRpc(input, output);

                var server = new RazorLanguageServerTarget(
                    jsonRpc,
                    logger,
                    projectSnapshotManagerDispatcher,
                    featureOptions,
                    configure);

                var razorLanguageServer = new RazorLanguageServer(server);

                await server.InitializeAsync();
                jsonRpc.StartListening();
                return razorLanguageServer;
            }

            private static StreamJsonRpc.JsonRpc CreateJsonRpc(Stream input, Stream output)
            {
                var messageFormatter = new JsonMessageFormatter();
                messageFormatter.JsonSerializer.AddVSInternalExtensionConverters();
                messageFormatter.JsonSerializer.Converters.RegisterRazorConverters();

                var jsonRpc = new JsonRpc(new HeaderDelimitedMessageHandler(output, input, messageFormatter));

                return jsonRpc;
            }

            internal T GetRequiredService<T>() where T : notnull
            {
                return _innerServer.GetRequiredService<T>();
            }

            public async ValueTask DisposeAsync()
            {
                await _innerServer.DisposeAsync();

                lock (_disposeLock)
                {
                    if (!_disposed)
                    {
                        _disposed = true;

                        TempDirectory.Instance.Dispose();
                    }
                }
            }

            internal RazorLanguageServerTarget GetInnerLanguageServerForTesting()
            {
                return _innerServer;
            }
        }
    }
}
