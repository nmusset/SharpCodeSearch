import * as vscode from 'vscode';

export function activate(context: vscode.ExtensionContext) {
    console.log('Sharp Code Search extension is now active');

    // Register search command
    const searchCommand = vscode.commands.registerCommand('sharpCodeSearch.search', () => {
        vscode.window.showInformationMessage('Sharp Code Search: Search command (not yet implemented)');
    });

    // Register replace command
    const replaceCommand = vscode.commands.registerCommand('sharpCodeSearch.replace', () => {
        vscode.window.showInformationMessage('Sharp Code Search: Replace command (not yet implemented)');
    });

    // Register pattern catalog command
    const catalogCommand = vscode.commands.registerCommand('sharpCodeSearch.catalog', () => {
        vscode.window.showInformationMessage('Sharp Code Search: Pattern Catalog command (not yet implemented)');
    });

    context.subscriptions.push(searchCommand, replaceCommand, catalogCommand);
}

export function deactivate() {
    // Cleanup if needed
}
