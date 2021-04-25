/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { RazorLanguageServerClient } from '../RazorLanguageServerClient';
import * as expansionTypes from './EmbeddedLanguageSpecExpansionTypes';
import * as vscode from 'vscode';
import { ResponseError } from 'vscode-languageclient';

export class DelegatingFileSystemProvider implements vscode.FileSystemProvider {
    private onDidChangeEmitter = new vscode.EventEmitter<vscode.FileChangeEvent[]>();
    private readonly watchedUris: { [uri: string]: string } = {};
    private subscriptionCount = 0;

    public readonly onDidChangeFile: vscode.Event<vscode.FileChangeEvent[]> = this.onDidChangeEmitter.event;

    constructor(private readonly serverClient: RazorLanguageServerClient) {
        this.serverClient.onRequest('fileSystem/didChangeFile', (params: expansionTypes.DidChangeFileParams) => {
            this.onDidChangeEmitter.fire(params.changes);
        });
    }


    public watch(uri: vscode.Uri, options: { recursive: boolean; excludes: string[]; }): vscode.Disposable {
        this.subscriptionCount++;
        const subscriptionId = this.subscriptionCount.toString();
        this.watchedUris[uri.toString()] = subscriptionId
        const watchParams: expansionTypes.WatchParams = {
            uri,
            subscriptionId,
            options
        };
        this.serverClient.sendNotification('fileSystem/watch', watchParams);

        const disposable = vscode.Disposable.from({
            dispose: () => {
                const stopWatchingParams: expansionTypes.StopWatchingParams = {
                    subscriptionId
                };
                this.serverClient.sendNotification('fileSystem/stopWatching', stopWatchingParams);
            },
        });

        return disposable;
    }
    public async stat(uri: vscode.Uri): Promise<vscode.FileStat> {
        const params: expansionTypes.FileStatParams = {
            uri: uri.toString(),
        };
        try {
            const response = await this.serverClient.sendRequest<expansionTypes.FileStatResponse>('fileSystem/stat', params);
            return response;
        } catch (error) {
            const convertedError = this.convertToException(uri, error);
            throw convertedError;
        }
    }
    public async readDirectory(uri: vscode.Uri): Promise<[string, vscode.FileType][]> {
        const params: expansionTypes.ReadDirectoryParams = {
            uri,
        };
        const response = await this.serverClient.sendRequest<expansionTypes.ReadDirectoryResponse>('fileSystem/stat', params);
        const convertedResponse: [string, vscode.FileType][] = [];
        for (const directory of response.children) {
            convertedResponse.push([directory.name, directory.type]);
        }

        return convertedResponse;
    }
    public async createDirectory(uri: vscode.Uri): Promise<void> {
        const params: expansionTypes.CreateDirectoryParams = {
            uri,
        };
        try {
            await this.serverClient.sendRequest<expansionTypes.FileStatResponse>('fileSystem/createDirectory', params);
        } catch (error) {
            const convertedError = this.convertToException(uri, error);
            throw convertedError;
        }
    }
    public async readFile(uri: vscode.Uri): Promise<Uint8Array> {
        const params: expansionTypes.ReadFileParams = {
            uri: uri.toString(),
        };

        try {
            const response = await this.serverClient.sendRequest<expansionTypes.ReadFileResponse>('fileSystem/readFile', params);
            var convertedResponse = this.convertFromBase64(response.content);
            return convertedResponse;
        } catch (error) {
            const convertedError = this.convertToException(uri, error);
            throw convertedError;
        }
    }
    public async writeFile(uri: vscode.Uri, content: Uint8Array, options: { create: boolean; overwrite: boolean; }): Promise<void> {
        const base64Content = this.convertToBase64(content);
        const params: expansionTypes.WriteFileParams = {
            uri,
            content: base64Content,
            options,
        };

        try {
            await this.serverClient.sendRequest('fileSystem/writeFile', params);
        } catch (error) {
            const convertedError = this.convertToException(uri, error);
            throw convertedError;
        }
    }
    public async delete(uri: vscode.Uri, options: { recursive: boolean; }): Promise<void> {
        const params: expansionTypes.DeleteFileParams = {
            uri,
            options,
        };

        try {
            await this.serverClient.sendRequest('fileSystem/delete', params);
        } catch (error) {
            const convertedError = this.convertToException(uri, error);
            throw convertedError;
        }
    }
    public async rename(oldUri: vscode.Uri, newUri: vscode.Uri, options: { overwrite: boolean; }): Promise<void> {
        const params: expansionTypes.RenameFileParams = {
            oldUri,
            newUri,
            options,
        };

        try {
            await this.serverClient.sendRequest('fileSystem/rename', params);
        } catch (error) {
            const convertedError = this.convertToException(oldUri, error);
            throw convertedError;
        }
    }

    private convertToException(requestedUri: vscode.Uri, responseError: ResponseError<void>) {
        // TODO: This isn't fully complete, we don't capture the server stack or properly represent the code for "Other" scenarios.

        const messageOrUri = responseError.message ? responseError.message : requestedUri;
        switch (responseError.code) {
            case expansionTypes.FileSystemErrorType.FileExists:
                return vscode.FileSystemError.FileExists(messageOrUri);
            case expansionTypes.FileSystemErrorType.FileIsADirectory:
                return vscode.FileSystemError.FileIsADirectory(messageOrUri);
            case expansionTypes.FileSystemErrorType.FileNotADirectory:
                return vscode.FileSystemError.FileNotADirectory(messageOrUri);
            case expansionTypes.FileSystemErrorType.FileNotFound:
                return vscode.FileSystemError.FileNotFound(messageOrUri);
            case expansionTypes.FileSystemErrorType.NoPermissions:
                return vscode.FileSystemError.NoPermissions(messageOrUri);
            case expansionTypes.FileSystemErrorType.Unavailable:
                return vscode.FileSystemError.Unavailable(messageOrUri);
            case expansionTypes.FileSystemErrorType.Other:
                return new vscode.FileSystemError(messageOrUri);
        }

        throw new Error(`Unknown error type ${responseError.code}: ${responseError.message}`);
    }

    private convertToBase64(uintArray: Uint8Array) {
        const buffer = Buffer.from(uintArray);
        const base64 = buffer.toString('base64');

        return base64;
    }

    private convertFromBase64(base64Content: string) {
        const buffer = Buffer.from(base64Content, 'base64')
        const uintArray = new Uint8Array(buffer);

        return uintArray;
    }
}