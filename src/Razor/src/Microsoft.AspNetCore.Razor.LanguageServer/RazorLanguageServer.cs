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
using Microsoft.AspNetCore.Razor.LanguageServer.Completion.Delegation;
using Microsoft.AspNetCore.Razor.LanguageServer.Debugging;
using Microsoft.AspNetCore.Razor.LanguageServer.Definition;
using Microsoft.AspNetCore.Razor.LanguageServer.Diagnostics;
using Microsoft.AspNetCore.Razor.LanguageServer.DocumentColor;
using Microsoft.AspNetCore.Razor.LanguageServer.DocumentPresentation;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.AspNetCore.Razor.LanguageServer.Folding;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.AspNetCore.Razor.LanguageServer.Hover;
using Microsoft.AspNetCore.Razor.LanguageServer.LinkedEditingRange;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Refactoring;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.AspNetCore.Razor.LanguageServer.Tooltip;
using Microsoft.AspNetCore.Razor.LanguageServer.WrapWithTag;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
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
            //services.AddSingleton<ILanguageServerSettingsManager, DefaultlanguageServerSettingsManager>();
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

            services.AddSingleton<DocumentVersionCache, DefaultDocumentVersionCache>();
            services.AddSingleton<ProjectSnapshotChangeTrigger>((services) => services.GetRequiredService<DocumentVersionCache>());

            services.AddSingleton<RemoteTextLoaderFactory, DefaultRemoteTextLoaderFactory>();
            services.AddSingleton<ProjectResolver, DefaultProjectResolver>();
            services.AddSingleton<DocumentResolver, DefaultDocumentResolver>();
            services.AddSingleton<RazorProjectService, DefaultRazorProjectService>();
            services.AddSingleton<ProjectSnapshotChangeTrigger, OpenDocumentGenerator>();
            services.AddSingleton<RazorDocumentMappingService, DefaultRazorDocumentMappingService>();
            services.AddSingleton<RazorFileChangeDetectorManager>();

            // Options
            services.AddSingleton<RazorConfigurationService, DefaultRazorConfigurationService>();
            services.AddSingleton<RazorLSPOptionsMonitor>();
            services.AddSingleton<IOptionsMonitor<RazorLSPOptions>, RazorLSPOptionsMonitor>();

            // File change listeners
            services.AddSingleton<IProjectConfigurationFileChangeListener, ProjectConfigurationStateSynchronizer>();
            services.AddSingleton<IProjectFileChangeListener, ProjectFileSynchronizer>();
            services.AddSingleton<IRazorFileChangeListener, RazorFileSynchronizer>();

            // File Change detectors
            services.AddSingleton<IFileChangeDetector, ProjectConfigurationFileChangeDetector>();
            services.AddSingleton<IFileChangeDetector, ProjectFileChangeDetector>();
            services.AddSingleton<IFileChangeDetector, RazorFileChangeDetector>();

            // Document processed listeners
            services.AddSingleton<DocumentProcessedListener, RazorDiagnosticsPublisher>();

            services.AddSingleton<ProjectSnapshotManagerAccessor, DefaultProjectSnapshotManagerAccessor>();
            services.AddSingleton<TagHelperFactsService, DefaultTagHelperFactsService>();
            services.AddSingleton<LSPTagHelperTooltipFactory, DefaultLSPTagHelperTooltipFactory>();
            services.AddSingleton<VSLSPTagHelperTooltipFactory, DefaultVSLSPTagHelperTooltipFactory>();

            // Completion
            services.AddSingleton<CompletionListCache>();
            services.AddSingleton<AggregateCompletionListProvider>();
            services.AddSingleton<CompletionListProvider, DelegatedCompletionListProvider>();
            services.AddSingleton<CompletionListProvider, RazorCompletionListProvider>();
            services.AddSingleton<DelegatedCompletionResponseRewriter, TextEditResponseRewriter>();
            services.AddSingleton<DelegatedCompletionResponseRewriter, DesignTimeHelperResponseRewriter>();

            services.AddSingleton<AggregateCompletionItemResolver>();
            services.AddSingleton<CompletionItemResolver, RazorCompletionItemResolver>();
            services.AddSingleton<CompletionItemResolver, DelegatedCompletionItemResolver>();
            services.AddSingleton<TagHelperCompletionService, LanguageServerTagHelperCompletionService>();
            services.AddSingleton<RazorCompletionFactsService, DefaultRazorCompletionFactsService>();
            services.AddSingleton<RazorCompletionItemProvider, DirectiveCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, DirectiveAttributeCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, DirectiveAttributeParameterCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, DirectiveAttributeTransitionCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, MarkupTransitionCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, TagHelperCompletionProvider>();

            // Auto insert
            services.AddSingleton<RazorOnAutoInsertProvider, CloseTextTagOnAutoInsertProvider>();
            services.AddSingleton<RazorOnAutoInsertProvider, AutoClosingTagOnAutoInsertProvider>();

            // Folding Range Providers
            services.AddSingleton<RazorFoldingRangeProvider, RazorCodeBlockFoldingProvider>();

            // Formatting
            services.AddSingleton<RazorFormattingService, DefaultRazorFormattingService>();

            // Formatting Passes
            services.AddSingleton<IFormattingPass, HtmlFormattingPass>();
            services.AddSingleton<IFormattingPass, CSharpFormattingPass>();
            services.AddSingleton<IFormattingPass, CSharpOnTypeFormattingPass>();
            services.AddSingleton<IFormattingPass, FormattingDiagnosticValidationPass>();
            services.AddSingleton<IFormattingPass, FormattingContentValidationPass>();
            services.AddSingleton<IFormattingPass, RazorFormattingPass>();

            // Razor Code actions
            services.AddSingleton<RazorCodeActionProvider, ExtractToCodeBehindCodeActionProvider>();
            services.AddSingleton<RazorCodeActionResolver, ExtractToCodeBehindCodeActionResolver>();
            services.AddSingleton<RazorCodeActionProvider, ComponentAccessibilityCodeActionProvider>();
            services.AddSingleton<RazorCodeActionResolver, CreateComponentCodeActionResolver>();
            services.AddSingleton<RazorCodeActionResolver, AddUsingsCodeActionResolver>();

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

            services.AddSingleton<ErrorReporter, LanguageServerErrorReporter>();

            services.AddSingleton<DocumentContextFactory, DefaultDocumentContextFactory>();
            services.AddSingleton<FilePathNormalizer>();
            services.AddSingleton<ProjectSnapshotManagerDispatcher, LSPProjectSnapshotManagerDispatcher>();
            services.AddSingleton<GeneratedDocumentPublisher, DefaultGeneratedDocumentPublisher>();
            services.AddSingleton<AdhocWorkspaceFactory, DefaultAdhocWorkspaceFactory>();
            services.AddSingleton<ProjectSnapshotChangeTrigger>((services) => services.GetRequiredService<GeneratedDocumentPublisher>());

            services.AddSingleton<WorkspaceSemanticTokensRefreshPublisher, DefaultWorkspaceSemanticTokensRefreshPublisher>();
            services.AddSingleton<ProjectSnapshotChangeTrigger, DefaultWorkspaceSemanticTokensRefreshTrigger>();

            services.AddSingleton<DocumentVersionCache, DefaultDocumentVersionCache>();
            services.AddSingleton<ProjectSnapshotChangeTrigger>((services) => services.GetRequiredService<DocumentVersionCache>());

            services.AddSingleton<RemoteTextLoaderFactory, DefaultRemoteTextLoaderFactory>();
            services.AddSingleton<ProjectResolver, DefaultProjectResolver>();
            services.AddSingleton<DocumentResolver, DefaultDocumentResolver>();
            services.AddSingleton<RazorProjectService, DefaultRazorProjectService>();
            services.AddSingleton<ProjectSnapshotChangeTrigger, OpenDocumentGenerator>();
            services.AddSingleton<RazorDocumentMappingService, DefaultRazorDocumentMappingService>();
            services.AddSingleton<RazorFileChangeDetectorManager>();

            services.AddSingleton<ClientNotifierServiceBase, DefaultClientNotifierService>();

            services.AddSingleton<IOnLanguageServerStarted, DefaultClientNotifierService>();

            // Options
            services.AddSingleton<RazorConfigurationService, DefaultRazorConfigurationService>();
            services.AddSingleton<RazorLSPOptionsMonitor>();
            services.AddSingleton<IOptionsMonitor<RazorLSPOptions>, RazorLSPOptionsMonitor>();

            // File change listeners
            services.AddSingleton<IProjectConfigurationFileChangeListener, ProjectConfigurationStateSynchronizer>();
            services.AddSingleton<IProjectFileChangeListener, ProjectFileSynchronizer>();
            services.AddSingleton<IRazorFileChangeListener, RazorFileSynchronizer>();

            // File Change detectors
            services.AddSingleton<IFileChangeDetector, ProjectConfigurationFileChangeDetector>();
            services.AddSingleton<IFileChangeDetector, ProjectFileChangeDetector>();
            services.AddSingleton<IFileChangeDetector, RazorFileChangeDetector>();

            // Document processed listeners
            services.AddSingleton<DocumentProcessedListener, RazorDiagnosticsPublisher>();
            services.AddSingleton<DocumentProcessedListener, GeneratedDocumentSynchronizer>();
            services.AddSingleton<DocumentProcessedListener, CodeDocumentReferenceHolder>();

            services.AddSingleton<ProjectSnapshotManagerAccessor, DefaultProjectSnapshotManagerAccessor>();
            services.AddSingleton<TagHelperFactsService, DefaultTagHelperFactsService>();
            services.AddSingleton<LSPTagHelperTooltipFactory, DefaultLSPTagHelperTooltipFactory>();
            services.AddSingleton<VSLSPTagHelperTooltipFactory, DefaultVSLSPTagHelperTooltipFactory>();

            // Completion
            services.AddSingleton<CompletionListCache>();
            services.AddSingleton<AggregateCompletionListProvider>();
            services.AddSingleton<CompletionListProvider, DelegatedCompletionListProvider>();
            services.AddSingleton<CompletionListProvider, RazorCompletionListProvider>();
            services.AddSingleton<DelegatedCompletionResponseRewriter, TextEditResponseRewriter>();
            services.AddSingleton<DelegatedCompletionResponseRewriter, DesignTimeHelperResponseRewriter>();

            services.AddSingleton<AggregateCompletionItemResolver>();
            services.AddSingleton<CompletionItemResolver, RazorCompletionItemResolver>();
            services.AddSingleton<CompletionItemResolver, DelegatedCompletionItemResolver>();
            services.AddSingleton<TagHelperCompletionService, LanguageServerTagHelperCompletionService>();
            services.AddSingleton<RazorCompletionFactsService, DefaultRazorCompletionFactsService>();
            services.AddSingleton<RazorCompletionItemProvider, DirectiveCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, DirectiveAttributeCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, DirectiveAttributeParameterCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, DirectiveAttributeTransitionCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, MarkupTransitionCompletionItemProvider>();
            services.AddSingleton<RazorCompletionItemProvider, TagHelperCompletionProvider>();

            // Auto insert
            services.AddSingleton<RazorOnAutoInsertProvider, CloseTextTagOnAutoInsertProvider>();
            services.AddSingleton<RazorOnAutoInsertProvider, AutoClosingTagOnAutoInsertProvider>();

            // Folding Range Providers
            services.AddSingleton<RazorFoldingRangeProvider, RazorCodeBlockFoldingProvider>();

            // Formatting
            services.AddSingleton<RazorFormattingService, DefaultRazorFormattingService>();

            // Formatting Passes
            services.AddSingleton<IFormattingPass, HtmlFormattingPass>();
            services.AddSingleton<IFormattingPass, CSharpFormattingPass>();
            services.AddSingleton<IFormattingPass, CSharpOnTypeFormattingPass>();
            services.AddSingleton<IFormattingPass, FormattingDiagnosticValidationPass>();
            services.AddSingleton<IFormattingPass, FormattingContentValidationPass>();
            services.AddSingleton<IFormattingPass, RazorFormattingPass>();

            // Razor Code actions
            services.AddSingleton<RazorCodeActionProvider, ExtractToCodeBehindCodeActionProvider>();
            services.AddSingleton<RazorCodeActionResolver, ExtractToCodeBehindCodeActionResolver>();
            services.AddSingleton<RazorCodeActionProvider, ComponentAccessibilityCodeActionProvider>();
            services.AddSingleton<RazorCodeActionResolver, CreateComponentCodeActionResolver>();
            services.AddSingleton<RazorCodeActionResolver, AddUsingsCodeActionResolver>();

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

            // Defaults: For when the caller hasn't provided them through the `configure` action.
            //services.TryAddSingleton<HostServicesProvider, DefaultHostServicesProvider>();

            AddHandlers(services, _featureOptions);

            var lspServices = new LspServices(services);

            return lspServices;

            void AddHandlers(IServiceCollection services, LanguageServerFeatureOptions featureOptions)
            {
                // Lifetime Endpoints
                AddHandler<InitializeHandler<InitializeParams, InitializeResult, RazorRequestContext>>(services);
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
                    AddRegisteringHandler<RazorCompletionResolveEndpoint>(services);
                }
                else
                {
                    AddRegisteringHandler<LegacyRazorCompletionEndpoint>(services);
                    AddRegisteringHandler<LegacyRazorCompletionResolveEndpoint>(services);
                }

                void AddRegisteringHandler<T>(IServiceCollection services) where T : class, IMethodHandler, IRegistrationExtension
                {
                    services.AddSingleton<IMethodHandler, T>();
                    services.AddSingleton<IRegistrationExtension, T>();
                }

                void AddHandler<T>(IServiceCollection services) where T : class, IMethodHandler
                {
                    if (typeof(T) is IRegistrationExtension)
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

                var jsonRpc = new StreamJsonRpc.JsonRpc(new HeaderDelimitedMessageHandler(output, input, messageFormatter));

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
