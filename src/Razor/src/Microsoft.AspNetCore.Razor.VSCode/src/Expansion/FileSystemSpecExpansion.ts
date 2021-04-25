/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import * as expansionTypes from './EmbeddedLanguageSpecExpansionTypes';
import { RazorLanguageServerClient } from '../RazorLanguageServerClient';
import { DelegatingFileSystemProvider } from './DelegatingFileSystemProvider';

export class FileSystemSpecExpansion implements vscode.Disposable {
    private fileSystemProvider: DelegatingFileSystemProvider | undefined;
    private fileSystemRegistration: vscode.Disposable | undefined;

    constructor(
        private readonly serverClient: RazorLanguageServerClient) {
    }

    public async initialize() {
        const serverCapabilities = <expansionTypes.FileSystemEnabledServerCapabilities>this.serverClient.serverCapabilities;
        const fileSystemCapabilities = serverCapabilities.fileSystem;

        if (!fileSystemCapabilities) {
            return;
        }

        this.fileSystemProvider = new DelegatingFileSystemProvider(this.serverClient);
        this.fileSystemRegistration = vscode.workspace.registerFileSystemProvider(
            fileSystemCapabilities.scheme,
            this.fileSystemProvider,
            {
                isCaseSensitive: fileSystemCapabilities.isCaseSensitive,
                isReadonly: fileSystemCapabilities.isReadonly,
            });
    }

    public register() {
    }

    public dispose() {
        if (this.fileSystemRegistration) {
            this.fileSystemRegistration.dispose();
        }
    }
}