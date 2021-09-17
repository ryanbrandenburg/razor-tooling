// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.Workspaces.Editor;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    /// <summary>
    /// Keeps track of accurate settings on the client side so we can easily retrieve the
    /// options later when the server sends us a workspace/configuration request.
    /// </summary>
    [Shared]
    [Export(typeof(RazorLSPClientOptionsMonitor))]
    internal class RazorLSPClientOptionsMonitor
    {
        private readonly AnalyzerConfigManager _analyzerConfigManager;

        [ImportingConstructor]
        public RazorLSPClientOptionsMonitor(AnalyzerConfigManager analyzerConfigManager)
        {
            if (analyzerConfigManager is null)
            {
                throw new ArgumentNullException(nameof(analyzerConfigManager));
            }

            _analyzerConfigManager = analyzerConfigManager;
            ToolsOptionsSettings = EditorSettings.Default;
        }

        public EditorSettings ToolsOptionsSettings { get; set; }

        public bool TryGetCurrentOptionsForDocument(string filePath, out EditorSettings settings)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            // If we have an editorconfig, it should take priority over Tools->Options values.
            var analyzerConfigSet = _analyzerConfigManager.GetCurrentAnalyzerConfigSet();

            // Fall back to Tools->Options values if no .editorconfig file is found.
            if (analyzerConfigSet is null)
            {
                settings = ToolsOptionsSettings;
                return false;
            }

            var analyzerConfigOptions = analyzerConfigSet.GetOptionsForSourcePath(filePath);
            settings = ConvertAnalyzerConfigOptionsToEditorSettings(analyzerConfigOptions, ToolsOptionsSettings);
            return true;
        }

        private static EditorSettings ConvertAnalyzerConfigOptionsToEditorSettings(
            AnalyzerConfigOptionsResult analyzerConfigOptions,
            EditorSettings toolsOptionsSettings)
        {
            var useTabs = toolsOptionsSettings.IndentWithTabs;
            var indentSize = toolsOptionsSettings.IndentSize;

            if (!analyzerConfigOptions.AnalyzerOptions.TryGetValue("indent_style", out var indentStyleStr))
            {
                if (indentStyleStr is "tab")
                {
                    useTabs = true;
                }
                else if (indentStyleStr is "space")
                {
                    useTabs = false;
                }
            }

            if (!analyzerConfigOptions.AnalyzerOptions.TryGetValue("indent_size", out var indentSizeStr) &&
                int.TryParse(indentSizeStr, out var parsedIndentSize))
            {
                indentSize = parsedIndentSize;
            }

            return new EditorSettings(useTabs, indentSize);
        }
    }
}
