// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using Moq;
using Xunit;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using System;
using CommonLanguageServerProtocol.Framework;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultWorkspaceDirectoryPathResolverTest
    {
        [Fact]
        public void Resolve_RootUriUnavailable_UsesRootPath()
        {
            // Arrange
            var expectedWorkspaceDirectory = "/testpath";
            var clientSettings = new InitializeParams()
            {
                RootPath = expectedWorkspaceDirectory
            };
            var server = new Mock<IInitializeManager<InitializeParams, InitializeResult>>(MockBehavior.Strict);
            server.Setup(m => m.GetInitializeParams()).Returns(clientSettings);
            var workspaceDirectoryPathResolver = new DefaultWorkspaceDirectoryPathResolver(server.Object);

            // Act
            var workspaceDirectoryPath = workspaceDirectoryPathResolver.Resolve();

            // Assert
            Assert.Equal(expectedWorkspaceDirectory, workspaceDirectoryPath);
        }

        [Fact]
        public void Resolve_RootUriPrefered()
        {
            // Arrange
            var initialWorkspaceDirectory = "testpath";
            var uriBuilder = new UriBuilder
            {
                Scheme = "file",
                Host = null,
                Path = initialWorkspaceDirectory,
            };
            var clientSettings = new InitializeParams()
            {
                RootPath = "/somethingelse",
                RootUri = uriBuilder.Uri,
            };
            var server = new Mock<IInitializeManager<InitializeParams, InitializeResult>>(MockBehavior.Strict);
            server.Setup(s => s.GetInitializeParams()).Returns(clientSettings);
            var workspaceDirectoryPathResolver = new DefaultWorkspaceDirectoryPathResolver(server.Object);

            // Act
            var workspaceDirectoryPath = workspaceDirectoryPathResolver.Resolve();

            // Assert
            var expectedWorkspaceDirectory = $"file://{initialWorkspaceDirectory}/";
            Assert.Equal(expectedWorkspaceDirectory, workspaceDirectoryPath);
        }
    }
}
