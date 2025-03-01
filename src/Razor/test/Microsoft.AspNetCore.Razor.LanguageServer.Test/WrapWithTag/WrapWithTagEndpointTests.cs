﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.EndpointContracts.WrapWithTag;
using Microsoft.AspNetCore.Razor.LanguageServer.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Protocol;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Moq;
using OmniSharp.Extensions.JsonRpc;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.WrapWithTag
{
    public class WrapWithTagEndpointTest : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_Html_ReturnsResult()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var uri = new Uri("file://path/test.razor");
            var documentContextFactory = CreateDocumentContextFactory(uri, codeDocument);
            var responseRouterReturns = new Mock<IResponseRouterReturns>(MockBehavior.Strict);
            responseRouterReturns
                .Setup(l => l.Returning<WrapWithTagResponse>(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WrapWithTagResponse());

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync(LanguageServerConstants.RazorWrapWithTagEndpoint, It.IsAny<WrapWithTagParamsBridge>()))
                .ReturnsAsync(responseRouterReturns.Object);

            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);
            var endpoint = new WrapWithTagEndpoint(
                languageServer.Object,
                documentContextFactory,
                documentMappingService,
                LoggerFactory);

            var wrapWithDivParams = new WrapWithTagParamsBridge(new TextDocumentIdentifier { Uri = uri })
            {
                Range = new Range { Start = new Position(0, 0), End = new Position(0, 2) },
            };

            // Act
            var result = await endpoint.Handle(wrapWithDivParams, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            languageServer.Verify();
        }

        [Fact]
        public async Task Handle_CSharp_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("@counter");
            var uri = new Uri("file://path/test.razor");
            var documentContextFactory = CreateDocumentContextFactory(uri, codeDocument);
            var responseRouterReturns = new Mock<IResponseRouterReturns>(MockBehavior.Strict);
            responseRouterReturns
                .Setup(l => l.Returning<WrapWithTagResponse>(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WrapWithTagResponse());

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            languageServer
                .Setup(l => l.SendRequestAsync(LanguageServerConstants.RazorWrapWithTagEndpoint, It.IsAny<WrapWithTagParamsBridge>()))
                .ReturnsAsync(responseRouterReturns.Object);

            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.CSharp, MockBehavior.Strict);
            var endpoint = new WrapWithTagEndpoint(
                languageServer.Object,
                documentContextFactory,
                documentMappingService,
                LoggerFactory);

            var wrapWithDivParams = new WrapWithTagParamsBridge(new TextDocumentIdentifier { Uri = uri })
            {
                Range = new Range { Start = new Position(0, 0), End = new Position(0, 2) },
            };

            // Act
            var result = await endpoint.Handle(wrapWithDivParams, CancellationToken.None);

            // Assert
            Assert.Null(result);
            languageServer.Verify();
        }

        [Fact]
        public async Task Handle_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.Create("<div></div>");
            var realUri = new Uri("file://path/test.razor");
            var missingUri = new Uri("file://path/nottest.razor");
            var documentContextFactory = CreateDocumentContextFactory(missingUri, codeDocument, documentFound: false);

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);

            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);
            var endpoint = new WrapWithTagEndpoint(
                languageServer.Object,
                documentContextFactory,
                documentMappingService,
                LoggerFactory);

            var wrapWithDivParams = new WrapWithTagParamsBridge(new TextDocumentIdentifier { Uri = missingUri })
            {
                Range = new Range { Start = new Position(0, 0), End = new Position(0, 2) },
            };

            // Act
            var result = await endpoint.Handle(wrapWithDivParams, CancellationToken.None);

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
            var documentContextFactory = CreateDocumentContextFactory(uri, codeDocument);

            var languageServer = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);

            var documentMappingService = Mock.Of<RazorDocumentMappingService>(
                s => s.GetLanguageKind(codeDocument, It.IsAny<int>(), It.IsAny<bool>()) == RazorLanguageKind.Html, MockBehavior.Strict);
            var endpoint = new WrapWithTagEndpoint(
                languageServer.Object,
                documentContextFactory,
                documentMappingService,
                LoggerFactory);

            var wrapWithDivParams = new WrapWithTagParamsBridge(new TextDocumentIdentifier { Uri = uri })
            {
                Range = new Range { Start = new Position(0, 0), End = new Position(0, 2) },
            };

            // Act
            var result = await endpoint.Handle(wrapWithDivParams, CancellationToken.None);

            // Assert
            Assert.Null(result);

            Func<int, int, int> x = (_, _) => { return 4; };
        }

        [Fact]
        public async Task CleanUpTextEdits_NoTilde()
        {
            var input = """
                @if (true)
                {
                }
                """;
            var expected = """
                <div>
                    @if (true)
                    {
                    }
                </div>
                """;

            var uri = new Uri("file://path.razor");
            var factory = CreateDocumentContextFactory(uri, input);
            var context = await factory.TryCreateAsync(uri, CancellationToken.None);
            Assert.NotNull(context);
            var inputSourceText = await context!.GetSourceTextAsync(CancellationToken.None);

            var computedEdits = new TextEdit[]
            {
                new TextEdit
                {
                    NewText="<div>\r\n    ",
                    Range = new Range { Start= new Position(0, 0), End = new Position(0, 0) }
                },
                new TextEdit
                {
                    NewText="    ",
                    Range = new Range { Start= new Position(1, 0), End = new Position(1, 0) }
                },
                new TextEdit
                {
                    NewText="    }\r\n</div>",
                    Range = new Range { Start= new Position(2, 0), End = new Position(2, 1) }
                }
            };

            var edits = await WrapWithTagEndpoint.CleanUpTextEditsAsync(context!, computedEdits, CancellationToken.None);
            Assert.Same(computedEdits, edits);

            var finalText = inputSourceText.WithChanges(edits.Select(e => e.AsTextChange(inputSourceText)));
            Assert.Equal(expected, finalText.ToString());
        }

        [Fact]
        public async Task CleanUpTextEdits_BadEditWithTilde()
        {
            var input = """
                @if (true)
                {
                }
                """;

            var expected = """
                <div>
                    @if (true)
                    {
                    }
                </div>
                """;

            var uri = new Uri("file://path.razor");
            var factory = CreateDocumentContextFactory(uri, input);
            var context = await factory.TryCreateAsync(uri, CancellationToken.None);
            Assert.NotNull(context);
            var inputSourceText = await context!.GetSourceTextAsync(CancellationToken.None);

            var computedEdits = new TextEdit[]
            {
                new TextEdit
                {
                    NewText="<div>\r\n    ",
                    Range = new Range { Start= new Position(0, 0), End = new Position(0, 0) }
                },
                new TextEdit
                {
                    NewText="    ",
                    Range = new Range { Start= new Position(1, 0), End = new Position(1, 0) }
                },
                new TextEdit
                {
                    // This is the problematic edit.. the close brace has been replaced with a tilde
                    NewText="    ~\r\n</div>",
                    Range = new Range { Start= new Position(2, 0), End = new Position(2, 1) }
                }
            };

            var edits = await WrapWithTagEndpoint.CleanUpTextEditsAsync(context!, computedEdits, CancellationToken.None);
            Assert.NotSame(computedEdits, edits);

            var finalText = inputSourceText.WithChanges(edits.Select(e => e.AsTextChange(inputSourceText)));
            Assert.Equal(expected, finalText.ToString());
        }

        [Fact]
        public async Task CleanUpTextEdits_GoodEditWithTilde()
        {
            var input = """
                @if (true)
                {
                ~
                """;

            var expected = """
                <div>
                    @if (true)
                    {
                    ~
                </div>
                """;

            var uri = new Uri("file://path.razor");
            var factory = CreateDocumentContextFactory(uri, input);
            var context = await factory.TryCreateAsync(uri, CancellationToken.None);
            Assert.NotNull(context);
            var inputSourceText = await context!.GetSourceTextAsync(CancellationToken.None);

            var computedEdits = new TextEdit[]
            {
                new TextEdit
                {
                    NewText="<div>\r\n    ",
                    Range = new Range { Start= new Position(0, 0), End = new Position(0, 0) }
                },
                new TextEdit
                {
                    NewText="    ",
                    Range = new Range { Start= new Position(1, 0), End = new Position(1, 0) }
                },
                new TextEdit
                {
                    // This looks like a bad edit, but the original source document had a tilde
                    NewText="    ~\r\n</div>",
                    Range = new Range { Start= new Position(2, 0), End = new Position(2, 1) }
                }
            };

            var edits = await WrapWithTagEndpoint.CleanUpTextEditsAsync(context!, computedEdits, CancellationToken.None);
            Assert.NotSame(computedEdits, edits);

            var finalText = inputSourceText.WithChanges(edits.Select(e => e.AsTextChange(inputSourceText)));
            Assert.Equal(expected, finalText.ToString());
        }
    }
}
