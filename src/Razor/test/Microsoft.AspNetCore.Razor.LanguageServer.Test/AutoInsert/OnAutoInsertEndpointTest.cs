// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class OnAutoInsertEndpointTest : LanguageServerTestBase
    {
        public OnAutoInsertEndpointTest()
        {
        }

        [Fact]
        public async Task Handle_SingleProvider_InvokesProvider()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var insertProvider = new TestOnAutoInsertProvider(">", canResolve: true, Logger);
            var endpoint = new OnAutoInsertEndpoint(new[] { insertProvider }, TestAdhocWorkspaceFactory.Instance);
            var @params = new OnAutoInsertParamsBridge()
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri, },
                Character = ">",
                Options = new FormattingOptions
                {
                    TabSize = 4,
                    InsertSpaces = true
                },
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(@params, requestContext, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(insertProvider.Called);
        }

        [Fact]
        public async Task Handle_MultipleProviderSameTrigger_UsesSuccessful()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var insertProvider1 = new TestOnAutoInsertProvider(">", canResolve: false, Logger)
            {
                ResolvedTextEdit = new TextEdit()
            };
            var insertProvider2 = new TestOnAutoInsertProvider(">", canResolve: true, Logger)
            {
                ResolvedTextEdit = new TextEdit()
            };
            var endpoint = new OnAutoInsertEndpoint(new[] { insertProvider1, insertProvider2 }, TestAdhocWorkspaceFactory.Instance);
            var @params = new OnAutoInsertParamsBridge()
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri, },
                Character = ">",
                Options = new FormattingOptions
                {
                    TabSize = 4,
                    InsertSpaces = true
                },
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(@params, requestContext, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(insertProvider1.Called);
            Assert.True(insertProvider2.Called);
            Assert.Same(insertProvider2.ResolvedTextEdit, result?.TextEdit);
        }

        [Fact]
        public async Task Handle_MultipleProviderSameTrigger_UsesFirstSuccessful()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var insertProvider1 = new TestOnAutoInsertProvider(">", canResolve: true, Logger)
            {
                ResolvedTextEdit = new TextEdit()
            };
            var insertProvider2 = new TestOnAutoInsertProvider(">", canResolve: true, Logger)
            {
                ResolvedTextEdit = new TextEdit()
            };
            var endpoint = new OnAutoInsertEndpoint(new[] { insertProvider1, insertProvider2 }, TestAdhocWorkspaceFactory.Instance);
            var @params = new OnAutoInsertParamsBridge()
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri, },
                Character = ">",
                Options = new FormattingOptions
                {
                    TabSize = 4,
                    InsertSpaces = true
                },
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(@params, requestContext, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(insertProvider1.Called);
            Assert.False(insertProvider2.Called);
            Assert.Same(insertProvider1.ResolvedTextEdit, result?.TextEdit);
        }

        [Fact]
        public async Task Handle_MultipleProviderUnmatchingTrigger_ReturnsNull()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var insertProvider1 = new TestOnAutoInsertProvider(">", canResolve: true, Logger);
            var insertProvider2 = new TestOnAutoInsertProvider("<", canResolve: true, Logger);
            var endpoint = new OnAutoInsertEndpoint(new[] { insertProvider1, insertProvider2 }, TestAdhocWorkspaceFactory.Instance);
            var @params = new OnAutoInsertParamsBridge()
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri, },
                Character = "!",
                Options = new FormattingOptions
                {
                    TabSize = 4,
                    InsertSpaces = true
                },
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(@params, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
            Assert.False(insertProvider1.Called);
            Assert.False(insertProvider2.Called);
        }

        [Fact]
        public async Task Handle_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var insertProvider = new TestOnAutoInsertProvider(">", canResolve: true, Logger);
            var endpoint = new OnAutoInsertEndpoint(new[] { insertProvider }, TestAdhocWorkspaceFactory.Instance);
            var uri = new Uri("file://path/test.razor");
            var @params = new OnAutoInsertParamsBridge()
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri, },
                Character = ">",
                Options = new FormattingOptions
                {
                    TabSize = 4,
                    InsertSpaces = true
                },
            };
            var requestContext = CreateRazorRequestContext(documentContext: null);

            // Act
            var result = await endpoint.HandleRequestAsync(@params, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
            Assert.False(insertProvider.Called);
        }

        [Fact]
        public async Task Handle_UnsupportedCodeDocument_ReturnsNull()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            codeDocument.SetUnsupported();
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var insertProvider = new TestOnAutoInsertProvider(">", canResolve: true, Logger);
            var endpoint = new OnAutoInsertEndpoint(new[] { insertProvider }, TestAdhocWorkspaceFactory.Instance);
            var @params = new OnAutoInsertParamsBridge()
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri, },
                Character = ">",
                Options = new FormattingOptions
                {
                    TabSize = 4,
                    InsertSpaces = true
                },
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(@params, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
            Assert.False(insertProvider.Called);
        }

        [Fact]
        public async Task Handle_NoApplicableProvider_CallsProviderAndReturnsNull()
        {
            // Arrange
            var codeDocument = CreateCodeDocument();
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var insertProvider = new TestOnAutoInsertProvider(">", canResolve: false, Logger);
            var endpoint = new OnAutoInsertEndpoint(new[] { insertProvider }, TestAdhocWorkspaceFactory.Instance);
            var @params = new OnAutoInsertParamsBridge()
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri, },
                Character = ">",
                Options = new FormattingOptions
                {
                    TabSize = 4,
                    InsertSpaces = true
                },
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(@params, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
            Assert.True(insertProvider.Called);
        }

        private class TestOnAutoInsertProvider : RazorOnAutoInsertProvider
        {
            private readonly bool _canResolve;

            public TestOnAutoInsertProvider(string triggerCharacter, bool canResolve, ILogger logger) : base(logger)
            {
                TriggerCharacter = triggerCharacter;
                _canResolve = canResolve;
            }

            public bool Called { get; private set; }

            public TextEdit? ResolvedTextEdit { get; set; }

            public override string TriggerCharacter { get; }

            // Disabling because [NotNullWhen] is available in two Assemblies and causes warnings
#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
            public override bool TryResolveInsertion(Position position, FormattingContext context, out TextEdit? edit, out InsertTextFormat format)
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
            {
                Called = true;
                edit = ResolvedTextEdit!;
                format = default;
                return _canResolve;
            }
        }

        private static RazorCodeDocument CreateCodeDocument()
        {
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var emptySourceDocument = RazorSourceDocument.Create(content: string.Empty, fileName: "testFile.razor");
            var syntaxTree = RazorSyntaxTree.Parse(emptySourceDocument);
            codeDocument.SetSyntaxTree(syntaxTree);
            return codeDocument;
        }
    }
}
