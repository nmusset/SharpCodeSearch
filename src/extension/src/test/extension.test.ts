import * as assert from 'assert';
import * as vscode from 'vscode';

suite('Extension Test Suite', () => {
    vscode.window.showInformationMessage('Start extension tests');

    test('Extension should be present', () => {
        assert.ok(vscode.extensions.getExtension('sharpcodesearch.sharp-code-search'));
    });

    test('Should register search command', async () => {
        const commands = await vscode.commands.getCommands(true);
        assert.ok(commands.includes('sharpCodeSearch.search'));
    });

    test('Should register replace command', async () => {
        const commands = await vscode.commands.getCommands(true);
        assert.ok(commands.includes('sharpCodeSearch.replace'));
    });

    test('Should register catalog command', async () => {
        const commands = await vscode.commands.getCommands(true);
        assert.ok(commands.includes('sharpCodeSearch.catalog'));
    });
});
