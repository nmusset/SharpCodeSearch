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
exports.BackendService = void 0;
const vscode = __importStar(require("vscode"));
const path = __importStar(require("path"));
const child_process_1 = require("child_process");
const util_1 = require("util");
const execAsync = (0, util_1.promisify)(child_process_1.exec);
class BackendService {
    backendPath;
    workspacePath;
    constructor(context) {
        // Determine backend executable path
        // In development, it's in src/backend/bin/Debug/net10.0/
        // In production, it would be bundled with the extension
        const extensionRoot = context.extensionPath;
        this.backendPath = this.findBackendExecutable(extensionRoot);
        // Get workspace path
        const workspaceFolders = vscode.workspace.workspaceFolders;
        if (workspaceFolders && workspaceFolders.length > 0) {
            this.workspacePath = workspaceFolders[0].uri.fsPath;
        }
        else {
            this.workspacePath = '';
        }
    }
    /**
     * Find the backend executable path
     */
    findBackendExecutable(extensionRoot) {
        // For development, the backend is in the parent directory structure
        const devPath = path.join(extensionRoot, '..', 'backend', 'bin', 'Debug', 'net10.0', 'SharpCodeSearch.exe');
        const devPathDll = path.join(extensionRoot, '..', 'backend', 'bin', 'Debug', 'net10.0', 'SharpCodeSearch.dll');
        // For production, it would be bundled
        const prodPath = path.join(extensionRoot, 'backend', 'SharpCodeSearch.exe');
        const prodPathDll = path.join(extensionRoot, 'backend', 'SharpCodeSearch.dll');
        // Return the first path that exists (we'll verify at runtime)
        // On Windows, prefer .exe, otherwise use .dll with dotnet
        if (process.platform === 'win32') {
            return devPath; // Will try .exe first
        }
        else {
            return devPathDll; // Use dotnet to run .dll
        }
    }
    /**
     * Execute a search with the given pattern
     */
    async search(pattern, options = {}) {
        if (!this.workspacePath) {
            throw new Error('No workspace folder is open. Please open a C# project first.');
        }
        try {
            const output = await this.executeBackend(pattern, options);
            return this.parseSearchResults(output);
        }
        catch (error) {
            throw this.createBackendError(error);
        }
    }
    /**
     * Execute the backend CLI with the given pattern
     */
    async executeBackend(pattern, options) {
        const args = [
            '--pattern', `"${this.escapeArgument(pattern)}"`,
            '--output', 'json'
        ];
        // Add workspace/file option
        if (this.workspacePath) {
            args.push('--file', `"${this.escapeArgument(this.workspacePath)}"`);
        }
        // Add additional options
        if (options.filePattern) {
            args.push('--file-pattern', `"${this.escapeArgument(options.filePattern)}"`);
        }
        // Build command
        let command;
        if (process.platform === 'win32' && this.backendPath.endsWith('.exe')) {
            command = `"${this.backendPath}" ${args.join(' ')}`;
        }
        else {
            // Use dotnet to run the .dll
            const dllPath = this.backendPath.replace('.exe', '.dll');
            command = `dotnet "${dllPath}" ${args.join(' ')}`;
        }
        // Execute with timeout
        const { stdout, stderr } = await execAsync(command, {
            cwd: this.workspacePath,
            timeout: 60000, // 60 second timeout
            maxBuffer: 10 * 1024 * 1024 // 10MB buffer
        });
        if (stderr) {
            console.error('Backend stderr:', stderr);
        }
        return stdout;
    }
    /**
     * Parse search results from JSON output
     */
    parseSearchResults(output) {
        if (!output || output.trim().length === 0) {
            return [];
        }
        try {
            // The backend should output JSON
            const data = JSON.parse(output);
            // Handle different response formats
            if (Array.isArray(data)) {
                return data.map(this.normalizeResult);
            }
            else if (data.results && Array.isArray(data.results)) {
                return data.results.map(this.normalizeResult);
            }
            else {
                console.warn('Unexpected backend output format:', data);
                return [];
            }
        }
        catch (error) {
            // If not JSON, try to parse line-by-line (fallback for development)
            console.warn('Failed to parse JSON output, using fallback parsing:', error);
            return this.parseFallbackOutput(output);
        }
    }
    /**
     * Normalize a single result object
     */
    normalizeResult(result) {
        return {
            file: result.file || result.File || result.filePath || '',
            line: parseInt(result.line || result.Line || result.lineNumber || '1'),
            column: parseInt(result.column || result.Column || result.columnNumber || '1'),
            code: result.code || result.Code || result.snippet || '',
            matchedText: result.matchedText || result.MatchedText || result.match || '',
            placeholders: result.placeholders || result.Placeholders || {}
        };
    }
    /**
     * Fallback parsing for non-JSON output (development/debugging)
     */
    parseFallbackOutput(output) {
        // This is a simple fallback for when the backend doesn't output JSON yet
        // It won't be used once the backend properly outputs JSON
        return [];
    }
    /**
     * Create a user-friendly error from backend error
     */
    createBackendError(error) {
        if (error.code === 'ENOENT') {
            return {
                message: 'Backend executable not found',
                details: 'The SharpCodeSearch backend could not be located. Please ensure the extension is properly installed.'
            };
        }
        else if (error.killed || error.signal === 'SIGTERM') {
            return {
                message: 'Search timed out',
                details: 'The search operation took too long and was cancelled. Try searching in a smaller scope.'
            };
        }
        else if (error.stderr) {
            return {
                message: 'Backend error',
                details: error.stderr
            };
        }
        else {
            return {
                message: 'An unexpected error occurred',
                details: error.message || String(error)
            };
        }
    }
    /**
     * Escape command line argument
     */
    escapeArgument(arg) {
        // Escape double quotes and backslashes
        return arg.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
    }
    /**
     * Verify backend is available
     */
    async verifyBackend() {
        try {
            let command;
            if (process.platform === 'win32' && this.backendPath.endsWith('.exe')) {
                command = `"${this.backendPath}" --help`;
            }
            else {
                const dllPath = this.backendPath.replace('.exe', '.dll');
                command = `dotnet "${dllPath}" --help`;
            }
            await execAsync(command, { timeout: 5000 });
            return true;
        }
        catch (error) {
            console.error('Backend verification failed:', error);
            return false;
        }
    }
}
exports.BackendService = BackendService;
//# sourceMappingURL=BackendService.js.map