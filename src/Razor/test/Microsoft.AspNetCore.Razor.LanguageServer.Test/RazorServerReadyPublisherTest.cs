﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    public class RazorServerReadyPublisherTest : LanguageServerTestBase
    {
        [Fact]
        public void ProjectSnapshotManager_WorkspaceNull_DoesNothing()
        {
            // Arrange
            var clientNotifierService = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);

            var projectManager = TestProjectSnapshotManager.Create(LegacyDispatcher);
            projectManager.AllowNotifyListeners = true;

            var document = TestDocumentSnapshot.Create("C:/file.cshtml");
            document.TryGetText(out var text);
            document.TryGetTextVersion(out var textVersion);
            var textAndVersion = TextAndVersion.Create(text, textVersion);

            // Act
            projectManager.ProjectAdded(document.ProjectInternal.HostProject);
            projectManager.DocumentAdded(document.ProjectInternal.HostProject, document.State.HostDocument, TextLoader.From(textAndVersion));

            // Assert
            // Should not have been called
            clientNotifierService.Verify();
        }

        [Fact]
        public void ProjectSnapshotManager_WorkspacePopulated_DoesNotFireTwice()
        {
            // Arrange
            var clientNotifierService = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);

            var projectManager = TestProjectSnapshotManager.Create(LegacyDispatcher);
            projectManager.AllowNotifyListeners = true;

            var document = TestDocumentSnapshot.Create("C:/file.cshtml");
            document.TryGetText(out var text);
            document.TryGetTextVersion(out var textVersion);
            var textAndVersion = TextAndVersion.Create(text, textVersion);

            projectManager.ProjectAdded(document.ProjectInternal.HostProject);

            // Act
            projectManager.ProjectWorkspaceStateChanged(document.ProjectInternal.HostProject.FilePath, CreateProjectWorkspace());

            clientNotifierService.VerifyAll();

            projectManager.ProjectWorkspaceStateChanged(document.ProjectInternal.HostProject.FilePath, CreateProjectWorkspace());

            // Assert
            clientNotifierService.VerifyAll();
        }

        private static ProjectWorkspaceState CreateProjectWorkspace()
        {
            var tagHelpers = new List<TagHelperDescriptor>();

            return new ProjectWorkspaceState(tagHelpers, default);
        }
    }
}
