﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.Protocol;
using Microsoft.AspNetCore.Razor.LanguageServer.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Diagnostics
{
    public class RazorDiagnosticsEndpointTest : LanguageServerTestBase
    {
        public RazorDiagnosticsEndpointTest()
        {
            MappingService = new DefaultRazorDocumentMappingService(TestLanguageServerFeatureOptions.Instance, new TestDocumentContextFactory(), LoggerFactory);
        }

        private RazorDocumentMappingService MappingService { get; }

        [Fact(Skip = "Debug.Fail doesn't work in CI")]
        public async Task Handle_DocumentResolveFailed_ThrowsDebugFail()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var documentContextFactory = new Mock<DocumentContextFactory>(MockBehavior.Strict);
            documentContextFactory.Setup(resolver => resolver.TryCreateAsync(documentPath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(value: null);

            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory.Object, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                RazorDocumentUri = documentPath,
                Diagnostics = Array.Empty<VSDiagnostic>()
            };

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () => await Task.Run(() => diagnosticsEndpoint.Handle(request, default)));
        }

        [Fact]
        public async Task Handle_DocumentVersionFailed_ReturnsNullDiagnostics()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument, documentFound: false);

            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] { new VSDiagnostic() { Range = new Range { Start = new Position(0, 10), End = new Position(0, 22) }, } },
                RazorDocumentUri = documentPath,
            };
            var expectedRange = new Range { Start = new Position(0, 4), End = new Position(0, 16) };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Null(response.Diagnostics);
            Assert.Null(response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_CSharp()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] { new VSDiagnostic() { Range = new Range { Start = new Position(0, 10), End = new Position(0, 22) } } },
                RazorDocumentUri = documentPath,
            };
            var expectedRange = new Range { Start = new Position(0, 4), End = new Position(0, 16) };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessRudeEditDiagnostics_StatementLiteral_CSharp()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "@{ void Foo() {} }",
                "   void Foo() {} ",
                new[] {
                    // " void Foo() {} "
                    new SourceMapping(
                        new SourceSpan(2, 15),
                        new SourceSpan(2, 15))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,

                // Rude edit diagnostics get mapped directly onto the Razor document via the corresponding "runtime" representation
                Diagnostics = new[] { new VSDiagnostic() { Code = "ENC123", Range = new Range { Start = new Position(0, 3), End = new Position(0, 16) } } },
                RazorDocumentUri = documentPath,
            };
            var expectedRange = new Range { Start = new Position(0, 3), End = new Position(0, 16) };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessRudeEditDiagnostics_ExpressionLiteral_CSharp()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "@Method((parameter) => {})",
                "__o = Method((parameter) => {})",
                new[] {
                    // "Method((parameter) => {})"
                    new SourceMapping(
                        new SourceSpan(1, 25),
                        new SourceSpan(6, 25))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,

                // Rude edit diagnostics get mapped directly onto the Razor document via the corresponding "runtime" representation
                Diagnostics = new[] { new VSDiagnostic() { Code = "ENC123", Range = new Range { Start = new Position(0, 13), End = new Position(0, 23) } } },
                RazorDocumentUri = documentPath,
            };
            var expectedRange = new Range { Start = new Position(0, 13), End = new Position(0, 23) };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessRudeEditDiagnostics_Unknown_CSharp()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                " @{ void Foo< @* Comment! *@ TValue>() {} }  ",
                "    void Foo<  TValue>() {} ",
                new[] {
                    // "Foo< "
                    new SourceMapping(
                        new SourceSpan(3, 11),
                        new SourceSpan(3, 11)),

                    // " TValue>"
                    new SourceMapping(
                        new SourceSpan(28, 14),
                        new SourceSpan(15, 13))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,

                // Rude edit diagnostics get mapped directly onto the Razor document via the corresponding "runtime" representation
                Diagnostics = new[] { new VSDiagnostic() { Code = "ENC123", Range = new Range { Start = new Position(0, 9), End = new Position(0, 36) } } },
                RazorDocumentUri = documentPath,
            };
            var expectedRange = new Range { Start = new Position(0, 1), End = new Position(0, 43) };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_FilterDiagnostics_CSharpInsideStyleBlockSpace()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<style> @DateTime.Now </style>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Range = new Range { Start = new Position(0, 7),End =  new Position(0, 7) },
                        Code = CSSErrorCodes.MissingSelectorBeforeCombinatorCode,
                        Severity = DiagnosticSeverity.Warning
                    }
                },
                RazorDocumentUri = documentPath,
            };
            var expectedRange = new Range { Start = new Position(0, 8),End =  new Position(0, 15) };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
        }

        [Fact]
        public async Task Handle_FilterDiagnostics_CSharpInsideStylePropertySpace()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<style> body { overflow: @DateTime.Now; } </style>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(26, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Range = new Range { Start = new Position(0, 25),End =  new Position(0, 38) },
                        Code = "CSS123456",
                        Severity = DiagnosticSeverity.Warning
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
        }

        [Fact]
        public async Task Handle_FilterDiagnostics_CSharpInsideStyleBlock()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<style> @DateTime.Now </style>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Range = new Range { Start = new Position(0, 8), End = new Position(0, 15) },
                        Code = CSSErrorCodes.MissingSelectorBeforeCombinatorCode,
                        Severity = DiagnosticSeverity.Warning
                    }
                },
                RazorDocumentUri = documentPath,
            };
            var expectedRange = new Range { Start = new Position(0, 8),End =  new Position(0, 15)};

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
        }

        [Fact]
        public async Task Handle_FilterDiagnostics_CSharpWarning()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Range = new Range { Start = new Position(0, 10), End = new Position(0, 22)},
                        Code = RazorDiagnosticsEndpoint.CSharpDiagnosticsToIgnore.First(),
                        Severity = DiagnosticSeverity.Warning
                    }
                },
                RazorDocumentUri = documentPath,
            };
                var expectedRange = new Range { Start = new Position(0, 4),End =  new Position(0, 16)};

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_UnbalancedHtmlTags()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p><</p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Code = RazorDiagnosticsEndpoint.CSharpDiagnosticsToIgnore.First(),
                        Severity = DiagnosticSeverity.Error
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Null(response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_DoNotFilterErrorDiagnostics_CSharpError()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Range = new Range { Start = new Position(0, 10),End =  new Position(0, 22)},
                        Code = RazorDiagnosticsEndpoint.CSharpDiagnosticsToIgnore.First(),
                        Severity = DiagnosticSeverity.Error
                    }
                },
                RazorDocumentUri = documentPath,
            };
            var expectedRange = new Range { Start = new Position(0, 4),End =  new Position(0, 16)};

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_CSharpWarning_Unmapped_ReturnsNoDiagnostics()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Severity = DiagnosticSeverity.Warning,
                        Range = new Range { Start = new Position(0, 0), End = new Position(0, 3)}
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_CSharpError_Unmapped_ReturnsUndefinedRangeDiagnostic()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Severity = DiagnosticSeverity.Error,
                        Range = new Range { Start = new Position(0, 0), End = new Position(0, 3)}
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RangeExtensions.UndefinedRange, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_CSharpError_CS1525_InAttribute_NoRazorDiagnostic_ReturnsDiagnostic()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p @onabort=\"\"></p>",
                "__o = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.ProgressEventArgs>(this, );",
                sourceMappings: Array.Empty<SourceMapping>());
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new TestRazorDiagnosticsEndpointWithoutRazorDiagnostic(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Code = "CS1525",
                        Severity = DiagnosticSeverity.Error,
                        Range = new Range { Start = new Position(0, 0),End =  new Position(0, 3)}
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RangeExtensions.UndefinedRange, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_CSharpError_CS1525_InAttribute_WithRazorDiagnostic_ReturnsNoDiagnostics()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.razor");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p @onabort=\"\"></p>",
                "__o = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.ProgressEventArgs>(this, );",
                sourceMappings: Array.Empty<SourceMapping>());
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new TestRazorDiagnosticsEndpointWithRazorDiagnostic(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Code = "CS1525",
                        Severity = DiagnosticSeverity.Error,
                        Range = new Range { Start = new Position(0, 128), End = new Position(0, 128)}
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_CSharpError_CS1525_NotInAttribute_ReturnsDiagnostics()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now)</p>",
                "var __o = DateTime.Now)",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 13),
                        new SourceSpan(10, 13))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new VSDiagnostic() {
                        Code = "CS1525",
                        Severity = DiagnosticSeverity.Error,
                        Range = new Range { Start = new Position(0, 12), End = new Position(0, 13)}
                    }
                },
                RazorDocumentUri = documentPath,
            };
            var expectedRange = new Range { Start = new Position(0, 6), End = new Position(0, 7)};

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now</p>");
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] { new VSDiagnostic() { Range = new Range { Start = new Position(0, 16),End =  new Position(0, 20)} } },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(request.Diagnostics[0].Range, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Razor()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now</p>");
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Razor,
                Diagnostics = new[] { new VSDiagnostic() { Range = new Range { Start = new Position(0, 1),End =  new Position(0, 3)} } },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(request.Diagnostics[0].Range, response.Diagnostics![0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Unsupported()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            codeDocument.SetUnsupported();
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] { new VSDiagnostic() { Range = new Range { Start = new Position(0, 10), End = new Position(0, 22)} } },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_TagHelperStartTag()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var addTagHelper = $"@addTagHelper *, TestAssembly{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(
                $"{addTagHelper}<button></button>",
                new[]
                {
                    GetButtonTagHelperDescriptor().Build()
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] { new VSDiagnostic() { Range = new Range { Start = new Position(1, 1),End =  new Position(1, 7)} } },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_TagHelperEndTag()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var addTagHelper = $"@addTagHelper *, TestAssembly{Environment.NewLine}";
            var codeDocument = CreateCodeDocument(
                $"{addTagHelper}<button></button>",
                new[]
                {
                    GetButtonTagHelperDescriptor().Build()
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] { new VSDiagnostic() { Range = new Range { Start = new Position(1, 10),End =  new Position(1, 17)} } },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_WithCSharpInAttribute_SingleDiagnostic()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p style=\"padding: @System.Math.PI px;\">Hello</p>",
                "var __o = System.Math.PI",
                new[] {
                    new SourceMapping(
                        new SourceSpan(20, 14),
                        new SourceSpan(10, 14))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] { new VSDiagnostic() { Range = new Range { Start = new Position(0, 18),End =  new Position(0, 19)} } },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_WithCSharpInAttribute_MultipleDiagnostics()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p style=\"abc;padding: @System.Math.PI px;abc;\">Hello</p>",
                "var __o = System.Math.PI",
                new[] {
                    new SourceMapping(
                        new SourceSpan(24, 14),
                        new SourceSpan(10, 14))
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new VSDiagnostic() { Range = new Range { Start = new Position(0, 1), End = new Position(0, 2)} },     // start of `p` tag
                    new VSDiagnostic() { Range = new Range { Start = new Position(0, 13),End =  new Position(0, 14)} },   // leading `abc`
                    new VSDiagnostic() { Range = new Range { Start = new Position(0, 25),End =  new Position(0, 26)} },   // `@`
                    new VSDiagnostic() { Range = new Range { Start = new Position(0, 45),End =  new Position(0, 46)} },   // trailing `abc`
                    new VSDiagnostic() { Range = new Range { Start = new Position(0, 55),End =  new Position(0, 57)} }    // in `Hello`
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Collection(response.Diagnostics,
                d => Assert.Equal(new Range { Start = new Position(0, 1),End =  new Position(0, 2)}, d.Range),
                d => Assert.Equal(new Range { Start = new Position(0, 55), End = new Position(0, 57)}, d.Range));
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_WithCSharpInTagHelperAttribute_MultipleDiagnostics()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");

            var descriptor = GetButtonTagHelperDescriptor();

            var addTagHelper = $"@addTagHelper *, TestAssembly{Environment.NewLine}";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                $"{addTagHelper}<button onactivate=\"Send\" disabled=\"@(Something)\">Hi</button>",
                $"var __o = Send;var __o = Something;",
                new[] {
                    new SourceMapping(
                        new SourceSpan(addTagHelper.Length + 20, 4),
                        new SourceSpan(addTagHelper.Length + 10, 4)),

                    new SourceMapping(
                        new SourceSpan(addTagHelper.Length + 38, 9),
                        new SourceSpan(addTagHelper.Length + 25, 9))
                },
                new[] {
                    descriptor.Build()
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new VSDiagnostic() { Range = new Range { Start = new Position(0, addTagHelper.Length + 20),End =  new Position(0, addTagHelper.Length + 25)} },
                    new VSDiagnostic() { Range = new Range { Start = new Position(0, addTagHelper.Length + 38),End =  new Position(0, addTagHelper.Length + 47)} }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_DisableInvalidNestingWarningInTagHelper()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var descriptor = GetButtonTagHelperDescriptor();

            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                $@"@addTagHelper *, TestAssembly
<button>
    @* Should NOT show warning *@
    <div>
        <option></option>
    </div>
</button>

@* Should show warning *@
<div>
    <option></option>
</div>",
                projectedCSharpSource: string.Empty,
                sourceMappings: Array.Empty<SourceMapping>(),
                new[] {
                    descriptor.Build()
                });
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new VSDiagnostic()
                    {
                        Code = HtmlErrorCodes.InvalidNestingErrorCode,
                        Range = new Range { Start = new Position(4, 8),End =  new Position(4, 17)}
                    },
                    new VSDiagnostic()
                    {
                        Code = HtmlErrorCodes.InvalidNestingErrorCode,
                        Range = new Range { Start = new Position(10, 4),End =  new Position(10, 13)}
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            var d = Assert.Single(response.Diagnostics);
            Assert.Equal(HtmlErrorCodes.InvalidNestingErrorCode, d.Code);
            Assert.Equal(10, d.Range.Start.Line);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_CSHTML_DoNotIgnoreMissingEndTagDiagnostic()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.cshtml");
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now");
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new VSDiagnostic()
                    {
                        Code = HtmlErrorCodes.MissingEndTagErrorCode,
                        Range = new Range { Start = new Position(0, 0),End =  new Position(0, 3)}
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            var returnedDiagnostic = Assert.Single(response.Diagnostics);
            Assert.NotNull(returnedDiagnostic.Code);
            Assert.True(returnedDiagnostic.Code!.Value.TryGetSecond(out var str));
            Assert.Equal(HtmlErrorCodes.MissingEndTagErrorCode, str);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_Razor_IgnoreMissingEndTagDiagnostic()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.razor");
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now", kind: FileKinds.Component);
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new VSDiagnostic()
                    {
                        Code = HtmlErrorCodes.MissingEndTagErrorCode,
                        Range = new Range { Start = new Position(0, 0),End =  new Position(0, 3)},
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_Razor_UnbalancedTags_UnexpectedEndTagErrorCode()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.razor");
            var codeDocument = CreateCodeDocument("<!body></body>", kind: FileKinds.Component);
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new VSDiagnostic()
                    {
                        Code = HtmlErrorCodes.UnexpectedEndTagErrorCode,
                        Range = new Range { Start = new Position(0, 7),End =  new Position(0, 9)},
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            var diagnostic = Assert.Single(response.Diagnostics);
            Assert.NotNull(diagnostic.Code);
            Assert.True(diagnostic.Code!.Value.TryGetSecond(out var str));
            Assert.Equal(HtmlErrorCodes.UnexpectedEndTagErrorCode, str);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_Razor_BodyTag_UnexpectedEndTagErrorCode()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.razor");
            var codeDocument = CreateCodeDocument("<html><!body><div></div></!body></html>", kind: FileKinds.Component);
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new VSDiagnostic()
                    {
                        Code = HtmlErrorCodes.UnexpectedEndTagErrorCode,
                        Range = new Range { Start = new Position(0, 7),End =  new Position(0, 9)}
                    },
                    new VSDiagnostic()
                    {
                        Code = HtmlErrorCodes.InvalidNestingErrorCode,
                        Range = new Range { Start = new Position(0, 14),End =  new Position(0, 16)},
                        Message = "'div' cannot be nested inside element 'html'.",
                    },
                    new VSDiagnostic()
                    {
                        Code = HtmlErrorCodes.TooFewElementsErrorCode,
                        Range = new Range { Start = new Position(0, 2),  End = new Position(0, 3)},
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html_Razor_UnexpectedEndTagErrorCode()
        {
            // Arrange
            var documentPath = new Uri("C:/path/to/document.razor");
            var codeDocument = CreateCodeDocument("<!body></!body>", kind: FileKinds.Component);
            var documentContextFactory = CreateDocumentContextFactory(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(documentContextFactory, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new VSDiagnostic()
                    {
                        Code = HtmlErrorCodes.UnexpectedEndTagErrorCode,
                        Range = new Range { Start = new Position(0, 7), End = new Position(0, 9)}
                    }
                },
                RazorDocumentUri = documentPath,
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
        }

        private static TagHelperDescriptorBuilder GetButtonTagHelperDescriptor()
        {
            var descriptor = TagHelperDescriptorBuilder.Create("ButtonTagHelper", "TestAssembly");
            descriptor.SetTypeName("TestNamespace.ButtonTagHelper");
            descriptor.TagMatchingRule(builder => builder.RequireTagName("button"));
            descriptor.BindAttribute(builder =>
                builder
                    .Name("onactivate")
                    .PropertyName("onactivate")
                    .TypeName(typeof(string).FullName));
            return descriptor;
        }

        private static RazorCodeDocument CreateCodeDocument(string text, IReadOnlyList<TagHelperDescriptor>? tagHelpers = null, string? kind = null)
        {
            tagHelpers ??= Array.Empty<TagHelperDescriptor>();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, kind ?? FileKinds.Legacy, Array.Empty<RazorSourceDocument>(), tagHelpers);
            return codeDocument;
        }

        private static RazorCodeDocument CreateCodeDocumentWithCSharpProjection(
            string razorSource,
            string projectedCSharpSource,
            IEnumerable<SourceMapping> sourceMappings,
            IReadOnlyList<TagHelperDescriptor>? tagHelpers = null)
        {
            var codeDocument = CreateCodeDocument(razorSource, tagHelpers);
            var csharpDocument = RazorCSharpDocument.Create(
                    projectedCSharpSource,
                    RazorCodeGenerationOptions.CreateDefault(),
                    Enumerable.Empty<RazorDiagnostic>(),
                    sourceMappings,
                    Enumerable.Empty<LinePragma>());
            codeDocument.SetCSharpDocument(csharpDocument);
            return codeDocument;
        }

        class TestRazorDiagnosticsEndpointWithRazorDiagnostic : RazorDiagnosticsEndpoint
        {
            public TestRazorDiagnosticsEndpointWithRazorDiagnostic(
                DocumentContextFactory documentContextFactory,
                RazorDocumentMappingService documentMappingService,
                ILoggerFactory loggerFactory)
                : base(documentContextFactory, documentMappingService, loggerFactory)
            {
            }

            internal override bool CheckIfDocumentHasRazorDiagnostic(RazorCodeDocument codeDocument, string razorDiagnosticCode)
            {
                return true;
            }
        }

        class TestRazorDiagnosticsEndpointWithoutRazorDiagnostic : RazorDiagnosticsEndpoint
        {
            public TestRazorDiagnosticsEndpointWithoutRazorDiagnostic(
                DocumentContextFactory documentContextFactory,
                RazorDocumentMappingService documentMappingService,
                ILoggerFactory loggerFactory)
                : base(documentContextFactory, documentMappingService, loggerFactory)
            {
            }

            internal override bool CheckIfDocumentHasRazorDiagnostic(RazorCodeDocument codeDocument, string razorDiagnosticCode)
            {
                return false;
            }
        }
    }
}
