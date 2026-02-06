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
exports.activate = activate;
exports.deactivate = deactivate;
const vscode = __importStar(require("vscode"));
const BackendService_1 = require("./BackendService");
const SearchCommand_1 = require("./SearchCommand");
async function activate(context) {
    console.log('Sharp Code Search extension is now active');
    // Initialize backend service
    const backendService = new BackendService_1.BackendService(context);
    // Verify backend is available
    const backendAvailable = await backendService.verifyBackend();
    if (!backendAvailable) {
        vscode.window.showWarningMessage('Sharp Code Search: Backend not found. Some features may not work. Please ensure the extension is properly built.');
    }
    // Register search command with the new webview implementation
    const searchCommand = (0, SearchCommand_1.registerSearchCommand)(context, backendService);
    // Register replace command (opens the search panel with focus on replace input)
    const replaceCommand = vscode.commands.registerCommand('sharpCodeSearch.replace', () => {
        // Open the search panel - the replace functionality is integrated there
        const panel = SearchCommand_1.SearchPanel.currentPanel;
        if (panel) {
            // If panel is already open, just bring it to focus
            vscode.commands.executeCommand('sharpCodeSearch.search');
        }
        else {
            // Open the panel
            vscode.commands.executeCommand('sharpCodeSearch.search');
        }
    });
    // Register pattern catalog command (placeholder for now)
    const catalogCommand = vscode.commands.registerCommand('sharpCodeSearch.catalog', () => {
        vscode.window.showInformationMessage('Sharp Code Search: Pattern Catalog command (not yet implemented)');
    });
    context.subscriptions.push(searchCommand, replaceCommand, catalogCommand);
    console.log('Sharp Code Search: All commands registered successfully');
}
function deactivate() {
    // Cleanup if needed
    console.log('Sharp Code Search extension is being deactivated');
}
//# sourceMappingURL=extension.js.map