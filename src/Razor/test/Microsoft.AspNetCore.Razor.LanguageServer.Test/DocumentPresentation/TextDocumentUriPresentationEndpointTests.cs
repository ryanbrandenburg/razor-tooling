﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Protocol;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.DocumentPresentation
{
    public class TextDocumentUriPresentationEndpointTests : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_SimpleComponent_ReturnsResult()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);

            var componentCodeDocument = TestRazorCodeDocument.Create("<div></div>");
            var droppedUri = new Uri("file:///c:/path/MyTagHelper.razor");
            var builder = TagHelperDescriptorBuilder.Create("MyTagHelper", "MyAssembly");
            builder.SetTypeNameIdentifier("MyTagHelper");
            var tagHelperDescriptor = builder.Build();

            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(
                s => s.TryGetTagHelperDescriptorAsync(It.IsAny<DocumentSnapshot>(), It.IsAny<CancellationToken>()) == Task.FromResult(tagHelperDescriptor),
                MockBehavior.Strict);

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                },
                Uris = new[]
                {
                    droppedUri
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("<MyTagHelper />", result!.DocumentChanges!.Value.First[0].Edits[0].NewText);
        }

        [Fact]
        public async Task Handle_SimpleComponentWithChildFile_ReturnsResult()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);

            var componentCodeDocument = TestRazorCodeDocument.Create("<div></div>");
            var droppedUri = new Uri("file:///c:/path/MyTagHelper.razor");
            var builder = TagHelperDescriptorBuilder.Create("MyTagHelper", "MyAssembly");
            builder.SetTypeNameIdentifier("MyTagHelper");
            var tagHelperDescriptor = builder.Build();

            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(
                s => s.TryGetTagHelperDescriptorAsync(It.IsAny<DocumentSnapshot>(), It.IsAny<CancellationToken>()) == Task.FromResult(tagHelperDescriptor),
                MockBehavior.Strict);

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                },
                Uris = new[]
                {
                    new Uri("file:///c:/path/MyTagHelper.razor.cs"),
                    new Uri("file:///c:/path/MyTagHelper.razor.css"),
                    droppedUri,
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("<MyTagHelper />", result!.DocumentChanges!.Value.First[0].Edits[0].NewText);
        }

        [Fact]
        public async Task Handle_ComponentWithRequiredAttribute_ReturnsResult()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);

            var componentCodeDocument = TestRazorCodeDocument.Create("<div></div>");
            var droppedUri = new Uri("file:///c:/path/MyTagHelper.razor");
            var builder = TagHelperDescriptorBuilder.Create("MyTagHelper", "MyAssembly");
            builder.SetTypeNameIdentifier("MyTagHelper");
            builder.BindAttribute(b =>
            {
                b.IsEditorRequired = true;
                b.Name = "MyAttribute";
            });
            builder.BindAttribute(b => b.Name = "MyNonRequiredAttribute");
            var tagHelperDescriptor = builder.Build();

            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(
                s => s.TryGetTagHelperDescriptorAsync(It.IsAny<DocumentSnapshot>(), It.IsAny<CancellationToken>()) == Task.FromResult(tagHelperDescriptor),
                MockBehavior.Strict);

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                },
                Uris = new[]
                {
                    droppedUri
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("<MyTagHelper MyAttribute=\"\" />", result!.DocumentChanges!.Value.First[0].Edits[0].NewText);
        }

        [Fact]
        public async Task Handle_NoTypeNameIdentifier_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);

            var componentCodeDocument = TestRazorCodeDocument.Create("<div></div>");
            var droppedUri = new Uri("file:///c:/path/MyTagHelper.razor");
            var builder = TagHelperDescriptorBuilder.Create("MyTagHelper", "MyAssembly");
            var tagHelperDescriptor = builder.Build();

            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(
                s => s.TryGetTagHelperDescriptorAsync(It.IsAny<DocumentSnapshot>(), It.IsAny<CancellationToken>()) == Task.FromResult(tagHelperDescriptor),
                MockBehavior.Strict);

            var response = (WorkspaceEdit?)null;

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync<IRazorPresentationParams, WorkspaceEdit?>(LanguageServerConstants.RazorUriPresentationEndpoint, It.IsAny<IRazorPresentationParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                },
                Uris = new[]
                {
                    droppedUri
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_MultipleUris_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);

            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(MockBehavior.Strict);

            var response = (WorkspaceEdit?)null;

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync<IRazorPresentationParams, WorkspaceEdit?>(LanguageServerConstants.RazorUriPresentationEndpoint, It.IsAny<IRazorPresentationParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                },
                Uris = new[]
                {
                    new Uri("file:///c:/path/SomeOtherFile.cs"),
                    new Uri("file:///c:/path/Bar.Foo"),
                    new Uri("file:///c:/path/MyTagHelper.razor"),
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_NotComponent_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);

            var droppedUri = new Uri("file:///c:/path/MyTagHelper.cshtml");
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(MockBehavior.Strict);

            var response = (WorkspaceEdit?)null;

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync<IRazorPresentationParams, WorkspaceEdit?>(LanguageServerConstants.RazorUriPresentationEndpoint, It.IsAny<IRazorPresentationParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                },
                Uris = new[]
                {
                    droppedUri
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_CSharp_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("@counter");
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var projectedRange = It.IsAny<Range>();
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.CSharp &&
                s.TryMapToProjectedDocumentRange(codeDocument, It.IsAny<Range>(), out projectedRange) == true, MockBehavior.Strict);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(MockBehavior.Strict);

            var response = (WorkspaceEdit?)null;

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync<IRazorPresentationParams, WorkspaceEdit?>(LanguageServerConstants.RazorUriPresentationEndpoint, It.IsAny<IRazorPresentationParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(MockBehavior.Strict);

            var response = (WorkspaceEdit?)null;

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync<IRazorPresentationParams, WorkspaceEdit?>(LanguageServerConstants.RazorUriPresentationEndpoint, It.IsAny<IRazorPresentationParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_UnsupportedCodeDocument_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            codeDocument.SetUnsupported();
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(MockBehavior.Strict);

            var response = new WorkspaceEdit();

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync<IRazorPresentationParams, WorkspaceEdit?>(LanguageServerConstants.RazorUriPresentationEndpoint, It.IsAny<IRazorPresentationParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_NoUris_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var uri = new Uri("file://path/test.razor");
            var documentContext = CreateDocumentContext(uri, codeDocument);
            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);
            var searchEngine = Mock.Of<RazorComponentSearchEngine>(MockBehavior.Strict);

            var response = (WorkspaceEdit?)null;

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync<IRazorPresentationParams, WorkspaceEdit?>(LanguageServerConstants.RazorUriPresentationEndpoint, It.IsAny<IRazorPresentationParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var endpoint = new TextDocumentUriPresentationEndpoint(
                documentMappingService,
                searchEngine,
                languageServer.Object,
                TestLanguageServerFeatureOptions.Instance);

            var parameters = new UriPresentationParams()
            {
                TextDocument = new TextDocumentIdentifier
                {
                    Uri = uri
                },
                Range = new Range
                {
                    Start = new Position(0, 1),
                    End = new Position(0, 2)
                }
            };
            var requestContext = CreateRazorRequestContext(documentContext);

            // Act
            var result = await endpoint.HandleRequestAsync(parameters, requestContext, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        private static DocumentContext CreateDocumentContext(Uri documentPath, RazorCodeDocument codeDocument)
        {
            var documentContext = SetupDocumentContextFactory(documentPath, codeDocument);

            return documentContext;

            static DocumentContext SetupDocumentContextFactory(Uri documentPath, RazorCodeDocument codeDocument)
            {
                var sourceTextChars = new char[codeDocument.Source.Length];
                codeDocument.Source.CopyTo(0, sourceTextChars, 0, codeDocument.Source.Length);
                var sourceText = SourceText.From(new string(sourceTextChars));
                var snapshot = Mock.Of<DocumentSnapshot>(document =>
                    document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                    document.GetTextAsync() == Task.FromResult(sourceText), MockBehavior.Strict);
                var version = 1337;
                var documentContext = new DocumentContext(documentPath, snapshot, version);

                return documentContext;
            }
        }
    }
}
