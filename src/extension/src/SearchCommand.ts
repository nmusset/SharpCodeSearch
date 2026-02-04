import * as vscode from 'vscode';
import * as path from 'path';
import { BackendService, SearchResult } from './BackendService';

export class SearchPanel {
    public static currentPanel: SearchPanel | undefined;
    private readonly _panel: vscode.WebviewPanel;
    private readonly _extensionUri: vscode.Uri;
    private readonly _backendService: BackendService;
    private _disposables: vscode.Disposable[] = [];

    public static createOrShow(extensionUri: vscode.Uri, backendService: BackendService) {
        const column = vscode.window.activeTextEditor
            ? vscode.window.activeTextEditor.viewColumn
            : undefined;

        // If we already have a panel, show it
        if (SearchPanel.currentPanel) {
            SearchPanel.currentPanel._panel.reveal(column);
            return;
        }

        // Otherwise, create a new panel
        const panel = vscode.window.createWebviewPanel(
            'sharpCodeSearch',
            'Sharp Code Search',
            column || vscode.ViewColumn.One,
            {
                enableScripts: true,
                retainContextWhenHidden: true,
                localResourceRoots: [
                    vscode.Uri.joinPath(extensionUri, 'out'),
                    vscode.Uri.joinPath(extensionUri, 'webview')
                ]
            }
        );

        SearchPanel.currentPanel = new SearchPanel(panel, extensionUri, backendService);
    }

    private constructor(panel: vscode.WebviewPanel, extensionUri: vscode.Uri, backendService: BackendService) {
        this._panel = panel;
        this._extensionUri = extensionUri;
        this._backendService = backendService;

        // Set the webview's initial html content
        this._update();

        // Listen for when the panel is disposed
        // This happens when the user closes the panel or when the panel is closed programmatically
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);

        // Handle messages from the webview
        this._panel.webview.onDidReceiveMessage(
            message => {
                this._handleMessage(message);
            },
            null,
            this._disposables
        );
    }

    /**
     * Handle messages from the webview
     */
    private async _handleMessage(message: any) {
        switch (message.type) {
            case 'search':
                await this._handleSearch(message.pattern, message.options);
                break;
            case 'navigateToMatch':
                await this._navigateToMatch(message.file, message.line, message.column);
                break;
        }
    }

    /**
     * Handle search request
     */
    private async _handleSearch(pattern: string, options: any) {
        try {
            // Send progress message
            this._panel.webview.postMessage({
                type: 'searchProgress',
                message: 'Searching for pattern...'
            });

            // Execute search
            const results = await this._backendService.search(pattern, options);

            // Send results back to webview
            this._panel.webview.postMessage({
                type: 'searchResults',
                results: results
            });

            // Show notification with result count
            if (results.length > 0) {
                vscode.window.showInformationMessage(
                    `Found ${results.length} match${results.length === 1 ? '' : 'es'}`
                );
            }
        } catch (error: any) {
            console.error('Search error:', error);

            // Send error to webview
            this._panel.webview.postMessage({
                type: 'searchError',
                error: error.message || String(error)
            });

            // Show error notification
            vscode.window.showErrorMessage(
                `Search failed: ${error.message || String(error)}`
            );
        }
    }

    /**
     * Navigate to a match location in the editor
     */
    private async _navigateToMatch(file: string, line: number, column: number) {
        try {
            // Resolve file path
            const workspaceFolders = vscode.workspace.workspaceFolders;
            let filePath: string;

            if (path.isAbsolute(file)) {
                filePath = file;
            } else if (workspaceFolders && workspaceFolders.length > 0) {
                filePath = path.join(workspaceFolders[0].uri.fsPath, file);
            } else {
                vscode.window.showErrorMessage('Cannot navigate: no workspace folder open');
                return;
            }

            // Open document
            const document = await vscode.workspace.openTextDocument(filePath);
            const editor = await vscode.window.showTextDocument(document);

            // Navigate to position (adjust for 0-based indexing)
            const position = new vscode.Position(Math.max(0, line - 1), Math.max(0, column - 1));
            const range = new vscode.Range(position, position);

            editor.selection = new vscode.Selection(position, position);
            editor.revealRange(range, vscode.TextEditorRevealType.InCenter);

        } catch (error) {
            console.error('Navigation error:', error);
            vscode.window.showErrorMessage(`Failed to navigate to ${file}:${line}:${column}`);
        }
    }

    /**
     * Update the webview content
     */
    private _update() {
        const webview = this._panel.webview;
        this._panel.title = 'Sharp Code Search';
        this._panel.webview.html = this._getHtmlForWebview(webview);
    }

    /**
     * Get the HTML content for the webview
     */
    private _getHtmlForWebview(webview: vscode.Webview): string {
        // Get paths to resources
        const webviewPath = vscode.Uri.joinPath(this._extensionUri, 'webview');

        const scriptUri = webview.asWebviewUri(vscode.Uri.joinPath(webviewPath, 'search.js'));
        const styleUri = webview.asWebviewUri(vscode.Uri.joinPath(webviewPath, 'search.css'));

        // Read the HTML template
        const htmlPath = vscode.Uri.joinPath(webviewPath, 'search.html');
        const fs = require('fs');
        let html = fs.readFileSync(htmlPath.fsPath, 'utf8');

        // Use a nonce to whitelist which scripts can be run
        const nonce = this._getNonce();

        // Replace placeholders
        html = html.replace(/{{cspSource}}/g, webview.cspSource);
        html = html.replace(/{{cssUri}}/g, styleUri.toString());
        html = html.replace(/{{scriptUri}}/g, scriptUri.toString());
        html = html.replace(/{{nonce}}/g, nonce);

        return html;
    }

    /**
     * Generate a nonce for CSP
     */
    private _getNonce(): string {
        let text = '';
        const possible = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
        for (let i = 0; i < 32; i++) {
            text += possible.charAt(Math.floor(Math.random() * possible.length));
        }
        return text;
    }

    /**
     * Dispose of the panel
     */
    public dispose() {
        SearchPanel.currentPanel = undefined;

        // Clean up our resources
        this._panel.dispose();

        while (this._disposables.length) {
            const disposable = this._disposables.pop();
            if (disposable) {
                disposable.dispose();
            }
        }
    }
}

/**
 * Register the search command
 */
export function registerSearchCommand(context: vscode.ExtensionContext, backendService: BackendService): vscode.Disposable {
    return vscode.commands.registerCommand('sharpCodeSearch.search', () => {
        SearchPanel.createOrShow(context.extensionUri, backendService);
    });
}
