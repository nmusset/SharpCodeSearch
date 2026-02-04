import * as vscode from 'vscode';
import { BackendService } from './BackendService';
import { registerSearchCommand } from './SearchCommand';

export async function activate(context: vscode.ExtensionContext) {
    console.log('Sharp Code Search extension is now active');

    // Initialize backend service
    const backendService = new BackendService(context);

    // Verify backend is available
    const backendAvailable = await backendService.verifyBackend();
    if (!backendAvailable) {
        vscode.window.showWarningMessage(
            'Sharp Code Search: Backend not found. Some features may not work. Please ensure the extension is properly built.'
        );
    }

    // Register search command with the new webview implementation
    const searchCommand = registerSearchCommand(context, backendService);

    // Register replace command (placeholder for now)
    const replaceCommand = vscode.commands.registerCommand('sharpCodeSearch.replace', () => {
        vscode.window.showInformationMessage('Sharp Code Search: Replace command (not yet implemented)');
    });

    // Register pattern catalog command (placeholder for now)
    const catalogCommand = vscode.commands.registerCommand('sharpCodeSearch.catalog', () => {
        vscode.window.showInformationMessage('Sharp Code Search: Pattern Catalog command (not yet implemented)');
    });

    context.subscriptions.push(searchCommand, replaceCommand, catalogCommand);

    console.log('Sharp Code Search: All commands registered successfully');
}

export function deactivate() {
    // Cleanup if needed
    console.log('Sharp Code Search extension is being deactivated');
}
