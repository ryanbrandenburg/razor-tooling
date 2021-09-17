// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RemoteProjectSnapshotProjectEngineFactory : DefaultProjectSnapshotProjectEngineFactory
    {
        public static readonly IFallbackProjectEngineFactory FallbackProjectEngineFactory = new FallbackProjectEngineFactory();

        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly RazorLSPOptionsMonitor _optionsMonitor;

        public RemoteProjectSnapshotProjectEngineFactory(FilePathNormalizer filePathNormalizer, RazorLSPOptionsMonitor optionsMonitor) :
            base(FallbackProjectEngineFactory, ProjectEngineFactories.Factories)
        {
            if (filePathNormalizer is null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (optionsMonitor is null)
            {
                throw new ArgumentNullException(nameof(optionsMonitor));
            }

            _filePathNormalizer = filePathNormalizer;
            _optionsMonitor = optionsMonitor;
        }

        public override RazorProjectEngine Create(
            RazorConfiguration configuration,
            string filePath,
            RazorProjectFileSystem fileSystem,
            Action<RazorProjectEngineBuilder> configure)
        {
            if (fileSystem is not DefaultRazorProjectFileSystem defaultFileSystem)
            {
                Debug.Fail("Unexpected file system.");
                return null;
            }

            var remoteFileSystem = new RemoteRazorProjectFileSystem(defaultFileSystem.Root, _filePathNormalizer);
            return base.Create(configuration, filePath, remoteFileSystem, Configure);

            void Configure(RazorProjectEngineBuilder builder)
            {
                configure(builder);
                builder.Features.Add(new RemoteCodeGenerationOptionsFeature(_optionsMonitor, filePath));
            }
        }

        private class RemoteCodeGenerationOptionsFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            private readonly RazorLSPOptionsMonitor _optionsMonitor;
            private readonly string _filePath;

            public RemoteCodeGenerationOptionsFeature(RazorLSPOptionsMonitor optionsMonitor, string filePath)
            {
                if (optionsMonitor is null)
                {
                    throw new ArgumentNullException(nameof(optionsMonitor));
                }

                if (filePath is null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                _optionsMonitor = optionsMonitor;
                _filePath = filePath;
            }

            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                // We don't need to explicitly subscribe to options changing because this method will be run on every parse.
                options.IndentSize = _optionsMonitor.GetCurrentOptions(_filePath).TabSize;
                options.IndentWithTabs = !_optionsMonitor.GetCurrentOptions(_filePath).InsertSpaces;
            }
        }
    }
}
