"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.SearchPanel = void 0;
exports.registerSearchCommand = registerSearchCommand;
const vscode = __importStar(require("vscode"));
const path = __importStar(require("path"));
class SearchPanel {
    static currentPanel;
    _panel;
    _extensionUri;
    _backendService;
    _disposables = [];
    static createOrShow(extensionUri, backendService) {
        const column = vscode.window.activeTextEditor
            ? vscode.window.activeTextEditor.viewColumn
            : undefined;
        // If we already have a panel, show it
        if (SearchPanel.currentPanel) {
            SearchPanel.currentPanel._panel.reveal(column);
            return;
        }
        // Otherwise, create a new panel
        const panel = vscode.window.createWebviewPanel('sharpCodeSearch', 'Sharp Code Search', column || vscode.ViewColumn.One, {
            enableScripts: true,
            retainContextWhenHidden: true,
            localResourceRoots: [
                vscode.Uri.joinPath(extensionUri, 'out'),
                vscode.Uri.joinPath(extensionUri, 'webview')
            ]
        });
        SearchPanel.currentPanel = new SearchPanel(panel, extensionUri, backendService);
    }
    constructor(panel, extensionUri, backendService) {
        this._panel = panel;
        this._extensionUri = extensionUri;
        this._backendService = backendService;
        // Set the webview's initial html content
        this._update();
        // Listen for when the panel is disposed
        // This happens when the user closes the panel or when the panel is closed programmatically
        this._panel.onDidDispose(() => this.dispose(), null, this._disposables);
        // Handle messages from the webview
        this._panel.webview.onDidReceiveMessage(message => {
            this._handleMessage(message);
        }, null, this._disposables);
    }
    /**
     * Handle messages from the webview
     */
    async _handleMessage(message) {
        switch (message.type) {
            case 'search':
                await this._handleSearch(message.pattern, message.options);
                break;
            case 'preview':
                await this._handlePreview(message.pattern, message.replacePattern, message.options);
                break;
            case 'apply':
                await this._handleApply(message.pattern, message.replacePattern, message.options);
                break;
            case 'navigateToMatch':
                await this._navigateToMatch(message.file, message.line, message.column);
                break;
        }
    }
    /**
     * Handle search request
     */
    async _handleSearch(pattern, options) {
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
                vscode.window.showInformationMessage(`Found ${results.length} match${results.length === 1 ? '' : 'es'}`);
            }
        }
        catch (error) {
            console.error('Search error:', error);
            // Send error to webview
            this._panel.webview.postMessage({
                type: 'searchError',
                error: error.message || String(error)
            });
            // Show error notification
            vscode.window.showErrorMessage(`Search failed: ${error.message || String(error)}`);
        }
    }
    /**
     * Handle preview request (search and replace without applying)
     */
    async _handlePreview(pattern, replacePattern, options) {
        try {
            // Execute search and replace (preview mode)
            const results = await this._backendService.searchAndReplace(pattern, replacePattern, options);
            // Send results back to webview
            this._panel.webview.postMessage({
                type: 'replacementResults',
                results: results
            });
            // Show notification
            if (results.length > 0) {
                vscode.window.showInformationMessage(`Preview: ${results.length} replacement${results.length === 1 ? '' : 's'}`);
            }
        }
        catch (error) {
            console.error('Preview error:', error);
            // Send error to webview
            this._panel.webview.postMessage({
                type: 'replacementError',
                error: error.message || String(error)
            });
            // Show error notification
            vscode.window.showErrorMessage(`Preview failed: ${error.message || String(error)}`);
        }
    }
    /**
     * Handle apply request (execute replacements on files)
     */
    async _handleApply(pattern, replacePattern, options) {
        try {
            // Execute replacements (apply mode)
            const results = await this._backendService.applyReplacements(pattern, replacePattern, options);
            // Send results back to webview
            this._panel.webview.postMessage({
                type: 'applicationResults',
                results: results
            });
            // Show notification
            const successCount = results.filter((r) => r.success).length;
            const errorCount = results.filter((r) => !r.success).length;
            if (successCount > 0) {
                vscode.window.showInformationMessage(`Applied ${successCount} replacement${successCount === 1 ? '' : 's'}${errorCount > 0 ? `, ${errorCount} error${errorCount === 1 ? '' : 's'}` : ''}`);
            }
            if (errorCount > 0) {
                vscode.window.showWarningMessage(`${errorCount} file${errorCount === 1 ? '' : 's'} could not be modified`);
            }
        }
        catch (error) {
            console.error('Apply error:', error);
            // Send error to webview
            this._panel.webview.postMessage({
                type: 'applicationError',
                error: error.message || String(error)
            });
            // Show error notification
            vscode.window.showErrorMessage(`Apply failed: ${error.message || String(error)}`);
        }
    }
    /**
     * Navigate to a match location in the editor
     */
    async _navigateToMatch(file, line, column) {
        try {
            // Resolve file path
            const workspaceFolders = vscode.workspace.workspaceFolders;
            let filePath;
            if (path.isAbsolute(file)) {
                filePath = file;
            }
            else if (workspaceFolders && workspaceFolders.length > 0) {
                filePath = path.join(workspaceFolders[0].uri.fsPath, file);
            }
            else {
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
        }
        catch (error) {
            console.error('Navigation error:', error);
            vscode.window.showErrorMessage(`Failed to navigate to ${file}:${line}:${column}`);
        }
    }
    /**
     * Update the webview content
     */
    _update() {
        const webview = this._panel.webview;
        this._panel.title = 'Sharp Code Search';
        this._panel.webview.html = this._getHtmlForWebview(webview);
    }
    /**
     * Get the HTML content for the webview
     */
    _getHtmlForWebview(webview) {
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
    _getNonce() {
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
    dispose() {
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
exports.SearchPanel = SearchPanel;
/**
 * Register the search command
 */
function registerSearchCommand(context, backendService) {
    return vscode.commands.registerCommand('sharpCodeSearch.search', () => {
        SearchPanel.createOrShow(context.extensionUri, backendService);
    });
}
//# sourceMappingURL=SearchCommand.js.map