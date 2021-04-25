/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import * as Is from './is';
import * as lsp from 'vscode-languageclient';
import { RazorLanguageServerClient } from '../RazorLanguageServerClient';
import { FileSystemSpecExpansion } from './FileSystemSpecExpansion';

export class EmbeddedLanguageSpecExpansion implements vscode.Disposable {
    private readonly fileSystemSpecExpansion: FileSystemSpecExpansion;

    constructor(
        private readonly serverClient: RazorLanguageServerClient) {
        this.fileSystemSpecExpansion = new FileSystemSpecExpansion(serverClient);
    }

    public async initialize() {
        await this.fileSystemSpecExpansion.initialize();
    }

    public register() {
        this.fileSystemSpecExpansion.register();
        this.serverClient.onRequest(
            'workspace/applyEdit2',
            async lspWorkspaceEditParams => {
                try {
                    var vscodeWorkspaceEdit = new vscode.WorkspaceEdit();
                    for (const documentEdit of lspWorkspaceEditParams.edit.documentChanges) {
                        const documentUri = vscode.Uri.parse(documentEdit.textDocument.uri);
                        for (const edit of documentEdit.edits) {
                            const editRange: vscode.Range = new vscode.Range(
                                new vscode.Position(edit.range.start.line, edit.range.start.character),
                                new vscode.Position(edit.range.end.line, edit.range.end.character));
                            vscodeWorkspaceEdit.replace(documentUri, editRange, edit.newText);
                        }
                    }

                    const applied = await vscode.workspace.applyEdit(vscodeWorkspaceEdit);
                    if (!applied) {
                        console.log('Failed to apply workspace edit');
                    }
                    return {
                        applied,
                    };
                } catch (error) {
                    throw error;
                }
            });
        this.serverClient.onRequest(
            'textDocument/open',
            async openTextDocumentParameters => {
                try {
                    const uri = vscode.Uri.parse(openTextDocumentParameters.textDocument.uri);
                    await vscode.workspace.openTextDocument(uri)
                } catch (error) {
                    throw error;
                }
            });

        this.serverClient.onRequest(
            'textDocument/close',
            async closeTextDocumentParameters => console.log(`Should be closing the text document ${closeTextDocumentParameters.textDocument.uri}`));

        this.serverClient.onRequest(
            'textDocument/hover',
            async hoverParams => {
                try {
                    const uri = vscode.Uri.parse(hoverParams.textDocument.uri);
                    const position = new vscode.Position(hoverParams.position.line, hoverParams.position.character);
                    const hovers = await vscode.commands.executeCommand<vscode.Hover[]>('vscode.executeHoverProvider', uri, position);
                    if (!hovers || hovers.length === 0) {
                        console.log('No hover returned');
                        return;
                    }

                    const lspHovers: lsp.Hover[] = [];
                    for (var hoverModel of hovers) {
                        let lspHoverContents: lsp.MarkupContent | lsp.MarkedString | lsp.MarkedString[] | undefined;
                        for (var i = 0; i < hoverModel.contents.length; i++) {
                            const value = hoverModel.contents[i];
                            if (Is.string(value)) {
                                lspHoverContents = <lsp.MarkupContent>{
                                    kind: lsp.MarkupKind.Markdown,
                                    value: value,
                                };
                            } else if (Is.languageBlock(value)) {
                                if (hoverModel.contents.length === 1) {
                                    lspHoverContents = <lsp.MarkedString>{
                                        language: value.language,
                                        value: value.value,
                                    };
                                } else {
                                    lspHoverContents = <lsp.MarkedString[]>(lspHoverContents || []);
                                    lspHoverContents.push(<lsp.MarkedString>{
                                        language: value.language,
                                        value: value.value,
                                    });
                                }
                            } else if (value.value && Is.string(value.value)) {
                                // The hell?
                                lspHoverContents = <lsp.MarkupContent>{
                                    kind: lsp.MarkupKind.Markdown,
                                    value: value.value,
                                };
                            }
                        }

                        if (!lspHoverContents) {
                            console.log('Could not convert hover contents');
                            return;
                        }

                        const lspHover: lsp.Hover = {
                            contents: lspHoverContents,
                        };

                        if (hoverModel.range) {
                            const lspRange: lsp.Range = {
                                start: {
                                    character: hoverModel.range.start.character,
                                    line: hoverModel.range.start.line,
                                },
                                end: {
                                    character: hoverModel.range.end.character,
                                    line: hoverModel.range.end.line,
                                }
                            };

                            lspHover.range = lspRange;
                        }

                        lspHovers.push(lspHover)
                    }

                    return lspHovers;
                } catch (error) {
                    console.log(error);
                }
            });


        this.serverClient.onRequest(
            'textDocument/completion',
            async completionParams => {
                try {
                    const uri = vscode.Uri.parse(completionParams.textDocument.uri);
                    const position = new vscode.Position(completionParams.position.line, completionParams.position.character);
                    const completionModel = await vscode.commands.executeCommand<vscode.CompletionList>('vscode.executeCompletionItemProvider', uri, position, completionParams.context.triggerCharacter);

                    if (!completionModel) {
                        console.log('No completion model');
                        return;
                    }

                    const lspCompletionItems: lsp.CompletionItem[] = [];
                    const lspCompletionList: lsp.CompletionList = {
                        isIncomplete: completionModel.isIncomplete || false,
                        items: lspCompletionItems,
                    };

                    for (const completionItem of completionModel.items) {
                        const lspCompletionItem: lsp.CompletionItem = {
                            label: completionItem.label,
                            commitCharacters: completionItem.commitCharacters,
                            filterText: completionItem.filterText,
                            insertText: completionItem.insertText instanceof vscode.SnippetString ? completionItem.insertText.value : completionItem.insertText,
                            kind: this.getLspCompletionItemKind(completionItem.kind),
                            sortText: completionItem.sortText,
                            preselect: completionItem.preselect,
                            insertTextFormat: (completionItem.insertText instanceof vscode.SnippetString) ? lsp.InsertTextFormat.Snippet : undefined,
                        };
                        lspCompletionItems.push(lspCompletionItem);
                    }

                    return lspCompletionList;
                } catch (error) {
                    console.log(error);
                }
            });
    }

    dispose() {
        this.fileSystemSpecExpansion.dispose();
    }

    getLspCompletionItemKind(vscodeKind?: vscode.CompletionItemKind) : lsp.CompletionItemKind {
        switch (vscodeKind) {
            case vscode.CompletionItemKind.Text:
                return lsp.CompletionItemKind.Text;
            case vscode.CompletionItemKind.Method:
                return lsp.CompletionItemKind.Method;
            case vscode.CompletionItemKind.Function:
                return lsp.CompletionItemKind.Function;
            case vscode.CompletionItemKind.Constructor:
                return lsp.CompletionItemKind.Constructor;
            case vscode.CompletionItemKind.Field:
                return lsp.CompletionItemKind.Field;
            case vscode.CompletionItemKind.Variable:
                return lsp.CompletionItemKind.Variable;
            case vscode.CompletionItemKind.Class:
                return lsp.CompletionItemKind.Class;
            case vscode.CompletionItemKind.Interface:
                return lsp.CompletionItemKind.Interface
            case vscode.CompletionItemKind.Module:
                return lsp.CompletionItemKind.Module;
            case vscode.CompletionItemKind.Property:
                return lsp.CompletionItemKind.Property;
            case vscode.CompletionItemKind.Unit :
                return lsp.CompletionItemKind.Unit;
            case vscode.CompletionItemKind.Value :
                return lsp.CompletionItemKind.Value;
            case vscode.CompletionItemKind.Enum :
                return lsp.CompletionItemKind.Enum;
            case vscode.CompletionItemKind.Keyword :
                return lsp.CompletionItemKind.Keyword;
            case vscode.CompletionItemKind.Snippet :
                return lsp.CompletionItemKind.Snippet;
            case vscode.CompletionItemKind.Color :
                return lsp.CompletionItemKind.Color;
            case vscode.CompletionItemKind.Reference :
                return lsp.CompletionItemKind.Reference;
            case vscode.CompletionItemKind.File :
                return lsp.CompletionItemKind.File;
            case vscode.CompletionItemKind.Folder :
                return lsp.CompletionItemKind.Folder;
            case vscode.CompletionItemKind.EnumMember :
                return lsp.CompletionItemKind.EnumMember;
            case vscode.CompletionItemKind.Constant :
                return lsp.CompletionItemKind.Constant;
            case vscode.CompletionItemKind.Struct :
                return lsp.CompletionItemKind.Struct;
            case vscode.CompletionItemKind.Event :
                return lsp.CompletionItemKind.Event;
            case vscode.CompletionItemKind.Operator :
                return lsp.CompletionItemKind.Operator;
            case vscode.CompletionItemKind.TypeParameter :
                return lsp.CompletionItemKind.TypeParameter;
            default:
                return lsp.CompletionItemKind.Text;
        }
    }

}
