/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ServerCapabilities } from 'vscode-languageclient';

export interface FileSystemEnabledServerCapabilities extends ServerCapabilities {
    readonly fileSystem?: FileSystemProviderOptions;
}

export interface FileSystemProviderOptions {
    /**
     * The uri-scheme the provider registers for
     */
    scheme: string;

    /**
     * Whether or not the file system is case sensitive.
     */
    isCaseSensitive?: boolean;

    /**
     * Whether or not the file system is readonly.
     */
    isReadonly?: boolean
}

export interface WatchParams {
    /**
     * The uri of the file or folder to be watched.
     */
    uri: vscode.Uri;

    /**
     * The subscription ID to be used in order to stop watching the provided file or folder uri via the [StopWatching](#stopWatching) notification.
     */
    subscriptionId: string;

    /**
     * Configures the watch
     */
    options: WatchFileOptions
}

export interface WatchFileOptions {
    /**
     * If a folder should be recursively subscribed to
     */
    recursive: boolean;

    /**
     * Folders or files to exclude from being watched.
     */
    excludes: string[];
}

/**
 * A notification to signal an unsubscribe from a corresponding [watch](#watch) request.
 */
export interface StopWatchingParams {
    /**
     * The subscription id.
     */
    subscriptionId: string;
}

export interface FileStatParams {
    /**
     * The uri to retrieve metadata about.
     */
    uri: string;
}

export interface FileStatResponse {
    /**
     * The type of the file, e.g. is a regular file, a directory, or symbolic link
     * to a file/directory.
     *
     * *Note:* This value might be a bitmask, e.g. `FileType.File | FileType.SymbolicLink`.
     */
    type: vscode.FileType;

    /**
     * The creation timestamp in milliseconds elapsed since January 1, 1970 00:00:00 UTC.
     */
    ctime: number;

    /**
     * The modification timestamp in milliseconds elapsed since January 1, 1970 00:00:00 UTC.
     *
     * *Note:* If the file changed, it is important to provide an updated `mtime` that advanced
     * from the previous value. Otherwise there may be optimizations in place that will not show
     * the updated file contents in an editor for example.
     */
    mtime: number;

    /**
     * The size in bytes.
     *
     * *Note:* If the file changed, it is important to provide an updated `size`. Otherwise there
     * may be optimizations in place that will not show the updated file contents in an editor for
     * example.
     */
    size: number;
}

export interface ReadDirectoryParams {
    /**
     * The uri of the folder.
     */
    uri: vscode.Uri;
}

export interface ReadDirectoryResponse {
    /**
     * An array of nodes that represent the directories children.
     */
    children: DirectoryChild[]
}

/**
 * A name/type item that represents a directory child node.
 */
export interface DirectoryChild {
    /**
     * The name of the node, e.g. a filename or directory name.
     */
    name: string;

    /**
     * The type of the file, e.g. is a regular file, a directory, or symbolic link to a file/directory.
     *
     * *Note:* This value might be a bitmask, e.g. `FileType.File | FileType.SymbolicLink`.
     */
    type: vscode.FileType;
}

/**
 * An event to signal that a resource has been created, changed, or deleted. This
 * event should fire for resources that are being [watched](#FileSystemProvider.watch)
 * by clients of this provider.
 *
 * *Note:* It is important that the metadata of the file that changed provides an
 * updated `mtime` that advanced from the previous value in the [stat](#FileStat) and a
 * correct `size` value. Otherwise there may be optimizations in place that will not show
 * the change in an editor for example.
 */
export interface DidChangeFileParams {
    /**
     * The change events.'
     */
    changes: FileChangeEvent[];
}

/**
 * The event filesystem providers must use to signal a file change.
 */
export interface FileChangeEvent {
    /**
     * The type of change.
     */
    uri: vscode.Uri;

    /**
     * The uri of the file that has changed.
     */
    type: vscode.FileChangeType
}

/**
 * Enumeration of file change types.
 */
export namespace FileChangeType {
    /**
     * The contents or metadata of a file have changed.
     */
    export const Changed = 1;

    /**
     * A file has been created.
     */
    export const Created = 2;

    /**
     * A file has been deleted.
     */
    export const Deleted = 3;
}

export interface CreateDirectoryParams {
    /**
     * The uri of the folder
     */
    uri: vscode.Uri;
}

export namespace FileSystemErrorType {
    /**
     * An error to signal that a file or folder wasn't found.
     */
    export const FileNotFound = 0;

    /**
     * An error to signal that a file or folder already exists, e.g. when creating but not overwriting a file.
     */
    export const FileExists = 1;

    /**
     * An error to signal that a file is not a folder.
     */
    export const FileNotADirectory = 2;

    /**
     * An error to signal that a file is a folder.
     */
    export const FileIsADirectory = 3;

    /**
     * An error to signal that an operation lacks required permissions.
     */
    export const NoPermissions = 4;

    /**
     * An error to signal that the file system is unavailable or too busy to complete a request.
     */
    export const Unavailable = 5;

    /**
     * A custom error.
     */
    export const Other = 1000;
}

export interface ReadFileParams {
    /**
     * The uri of the folder
     */
    uri: string;
}

export interface ReadFileResponse {
    /**
     * The entire contents of the file `base64` encoded.
     */
    content: string;
}

export interface WriteFileParams {
    /**
     * The uri of the file to write
     */
    uri: vscode.Uri;

    /**
     * The new content of the file `base64` encoded.
     */
    content: string;

    /**
     * Options to define if missing files should or must be created.
     */
    options: WriteFileOptions
}

export interface WriteFileOptions {
    /**
     * If a new file should be created.
     */
    create: boolean;

    /**
     * If a pre-existing file should be overwritten.
     */
    overwrite: boolean;
}

export interface DeleteFileParams {
    /**
     * The uri of the file or folder to delete
     */
    uri: vscode.Uri;

    /**
     * Defines if deletion of folders is recursive.
     */
    options: DeleteFileOptions
}

export interface DeleteFileOptions {
    /**
     * If a folder should be recursively deleted.
     */
    recursive: boolean;
}

export interface RenameFileParams {
    /**
     * The existing file.
     */
    oldUri: vscode.Uri;

    /**
     * The new location.
     */
    newUri: vscode.Uri;

    /**
     * Defines if existing files should be overwritten.
     */
    options: RenameFileOptions
}

export interface RenameFileOptions {
    /**
     * If existing files should be overwritten.
     */
    overwrite: boolean;
}