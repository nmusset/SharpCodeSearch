import * as vscode from 'vscode';
import * as path from 'path';
import { exec } from 'child_process';
import { promisify } from 'util';

const execAsync = promisify(exec);

export interface SearchOptions {
    matchCase?: boolean;
    wholeWord?: boolean;
    filePattern?: string;
}

export interface SearchResult {
    file: string;
    line: number;
    column: number;
    code: string;
    matchedText: string;
    placeholders?: Record<string, string>;
}

export interface ReplacementResult {
    filePath: string;
    line: number;
    column: number;
    originalCode: string;
    replacementCode: string;
    placeholders?: Record<string, string>;
}

export interface ApplicationResult {
    filePath: string;
    replacementsApplied: number;
    success: boolean;
    error?: string;
}

export interface BackendError {
    message: string;
    details?: string;
}

export class BackendService {
    private backendPath: string;
    private workspacePath: string;

    constructor(context: vscode.ExtensionContext) {
        // Determine backend executable path
        // In development, it's in src/backend/bin/Debug/net10.0/
        // In production, it would be bundled with the extension
        const extensionRoot = context.extensionPath;
        this.backendPath = this.findBackendExecutable(extensionRoot);

        // Get workspace path
        const workspaceFolders = vscode.workspace.workspaceFolders;
        if (workspaceFolders && workspaceFolders.length > 0) {
            this.workspacePath = workspaceFolders[0].uri.fsPath;
        } else {
            this.workspacePath = '';
        }
    }

    /**
     * Find the backend executable path
     */
    private findBackendExecutable(extensionRoot: string): string {
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
        } else {
            return devPathDll; // Use dotnet to run .dll
        }
    }

    /**
     * Execute a search with the given pattern
     */
    async search(pattern: string, options: SearchOptions = {}): Promise<SearchResult[]> {
        if (!this.workspacePath) {
            throw new Error('No workspace folder is open. Please open a C# project first.');
        }

        try {
            const output = await this.executeBackend(pattern, options);
            return this.parseSearchResults(output);
        } catch (error) {
            throw this.createBackendError(error);
        }
    }

    /**
     * Execute search and replace (preview mode - no files are modified)
     */
    async searchAndReplace(pattern: string, replacePattern: string, options: SearchOptions = {}): Promise<ReplacementResult[]> {
        if (!this.workspacePath) {
            throw new Error('No workspace folder is open. Please open a C# project first.');
        }

        try {
            const output = await this.executeSearchAndReplaceBackend(pattern, replacePattern, options, false);
            return this.parseReplacementResults(output);
        } catch (error) {
            throw this.createBackendError(error);
        }
    }

    /**
     * Apply replacements to files (actually modifies files)
     */
    async applyReplacements(pattern: string, replacePattern: string, options: SearchOptions = {}): Promise<ApplicationResult[]> {
        if (!this.workspacePath) {
            throw new Error('No workspace folder is open. Please open a C# project first.');
        }

        try {
            const output = await this.executeSearchAndReplaceBackend(pattern, replacePattern, options, true);
            return this.parseApplicationResults(output);
        } catch (error) {
            throw this.createBackendError(error);
        }
    }

    /**
     * Execute the backend CLI with the given pattern
     */
    private async executeBackend(pattern: string, options: SearchOptions): Promise<string> {
        const args = [
            '--pattern', this.quoteArgument(pattern),
            '--output', 'json'
        ];

        // Add workspace option for workspace-level search
        if (this.workspacePath) {
            args.push('--workspace', this.quoteArgument(this.workspacePath));
        }

        // Add additional options
        if (options.filePattern) {
            args.push('--file-filter', this.quoteArgument(options.filePattern));
        }

        // Build command
        let command: string;
        if (process.platform === 'win32' && this.backendPath.endsWith('.exe')) {
            command = `"${this.backendPath}" ${args.join(' ')}`;
        } else {
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
     * Execute backend with search and replace pattern (preview or apply)
     */
    private async executeSearchAndReplaceBackend(pattern: string, replacePattern: string, options: SearchOptions, apply: boolean): Promise<string> {
        const args = [
            '--pattern', this.quoteArgument(pattern),
            '--replace', this.quoteArgument(replacePattern),
            '--output', 'json'
        ];

        // Add apply flag if requested
        if (apply) {
            args.push('--apply');
        }

        // Add workspace option for workspace-level search
        if (this.workspacePath) {
            args.push('--workspace', this.quoteArgument(this.workspacePath));
        }

        // Add additional options
        if (options.filePattern) {
            args.push('--file-filter', this.quoteArgument(options.filePattern));
        }

        // Build command
        let command: string;
        if (process.platform === 'win32' && this.backendPath.endsWith('.exe')) {
            command = `"${this.backendPath}" ${args.join(' ')}`;
        } else {
            // Use dotnet to run the .dll
            const dllPath = this.backendPath.replace('.exe', '.dll');
            command = `dotnet "${dllPath}" ${args.join(' ')}`;
        }

        // Execute with timeout
        const { stdout, stderr } = await execAsync(command, {
            cwd: this.workspacePath,
            timeout: 120000, // 120 second timeout for replacements
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
    private parseSearchResults(output: string): SearchResult[] {
        if (!output || output.trim().length === 0) {
            return [];
        }

        try {
            // The backend should output JSON
            const data = JSON.parse(output);

            // Handle different response formats
            if (Array.isArray(data)) {
                return data.map(this.normalizeResult);
            } else if (data.matches && Array.isArray(data.matches)) {
                return data.matches.map(this.normalizeResult);
            } else if (data.results && Array.isArray(data.results)) {
                return data.results.map(this.normalizeResult);
            } else {
                console.warn('Unexpected backend output format:', data);
                return [];
            }
        } catch (error) {
            // If not JSON, try to parse line-by-line (fallback for development)
            console.warn('Failed to parse JSON output, using fallback parsing:', error);
            return this.parseFallbackOutput(output);
        }
    }

    /**
     * Normalize a single result object
     */
    private normalizeResult(result: any): SearchResult {
        return {
            file: result.file || result.File || result.filePath || '',
            line: parseInt(result.line || result.Line || result.lineNumber || '1'),
            column: parseInt(result.column || result.Column || result.columnNumber || '1'),
            code: result.code || result.Code || result.matchedCode || result.snippet || '',
            matchedText: result.matchedText || result.MatchedText || result.match || result.matchedCode || '',
            placeholders: result.placeholders || result.Placeholders || {}
        };
    }

    /**
     * Parse replacement results from JSON output
     */
    private parseReplacementResults(output: string): ReplacementResult[] {
        if (!output || output.trim().length === 0) {
            return [];
        }

        try {
            const data = JSON.parse(output);

            // Handle different response formats
            if (Array.isArray(data)) {
                return data.map(this.normalizeReplacementResult);
            } else if (data.replacements && Array.isArray(data.replacements)) {
                return data.replacements.map(this.normalizeReplacementResult);
            } else if (data.results && Array.isArray(data.results)) {
                return data.results.map(this.normalizeReplacementResult);
            } else {
                console.warn('Unexpected backend output format:', data);
                return [];
            }
        } catch (error) {
            console.warn('Failed to parse replacement results:', error);
            return [];
        }
    }

    /**
     * Normalize a single replacement result object
     */
    private normalizeReplacementResult(result: any): ReplacementResult {
        return {
            filePath: result.filePath || result.FilePath || result.file || '',
            line: parseInt(result.line || result.Line || result.lineNumber || '1'),
            column: parseInt(result.column || result.Column || result.columnNumber || '1'),
            originalCode: result.originalCode || result.OriginalCode || '',
            replacementCode: result.replacementCode || result.ReplacementCode || '',
            placeholders: result.placeholders || result.Placeholders || {}
        };
    }

    /**
     * Parse application results from backend output
     */
    private parseApplicationResults(output: string): ApplicationResult[] {
        if (!output || output.trim().length === 0) {
            return [];
        }

        try {
            const data = JSON.parse(output);

            // Handle different response formats
            if (Array.isArray(data)) {
                return data.map(this.normalizeApplicationResult);
            } else if (data.applicationResults && Array.isArray(data.applicationResults)) {
                return data.applicationResults.map(this.normalizeApplicationResult);
            } else if (data.results && Array.isArray(data.results)) {
                return data.results.map(this.normalizeApplicationResult);
            } else {
                console.warn('Unexpected application results format:', data);
                return [];
            }
        } catch (error) {
            console.warn('Failed to parse application results:', error);
            return [];
        }
    }

    /**
     * Normalize a single application result object
     */
    private normalizeApplicationResult(result: any): ApplicationResult {
        return {
            filePath: result.filePath || result.FilePath || '',
            replacementsApplied: parseInt(result.replacementsApplied || result.ReplacementsApplied || '0'),
            success: result.success !== false && !result.error,
            error: result.error || result.Error || undefined
        };
    }

    /**
     * Fallback parsing for non-JSON output (development/debugging)
     */
    private parseFallbackOutput(output: string): SearchResult[] {
        // This is a simple fallback for when the backend doesn't output JSON yet
        // It won't be used once the backend properly outputs JSON
        return [];
    }

    /**
     * Create a user-friendly error from backend error
     */
    private createBackendError(error: any): BackendError {
        if (error.code === 'ENOENT') {
            return {
                message: 'Backend executable not found',
                details: 'The SharpCodeSearch backend could not be located. Please ensure the extension is properly installed.'
            };
        } else if (error.killed || error.signal === 'SIGTERM') {
            return {
                message: 'Search timed out',
                details: 'The search operation took too long and was cancelled. Try searching in a smaller scope.'
            };
        } else if (error.stderr) {
            return {
                message: 'Backend error',
                details: error.stderr
            };
        } else {
            return {
                message: 'An unexpected error occurred',
                details: error.message || String(error)
            };
        }
    }

    /**
     * Quote a command line argument for safe passing to shell
     */
    private quoteArgument(arg: string): string {
        // If argument contains spaces or special characters, wrap in quotes
        // Escape any double quotes inside the argument
        if (arg.includes(' ') || arg.includes('"') || arg.includes('$')) {
            return `"${arg.replace(/"/g, '\\"')}"`;
        }
        return arg;
    }

    /**
     * Verify backend is available
     */
    async verifyBackend(): Promise<boolean> {
        try {
            let command: string;
            if (process.platform === 'win32' && this.backendPath.endsWith('.exe')) {
                command = `"${this.backendPath}" --help`;
            } else {
                const dllPath = this.backendPath.replace('.exe', '.dll');
                command = `dotnet "${dllPath}" --help`;
            }

            await execAsync(command, { timeout: 5000 });
            return true;
        } catch (error) {
            console.error('Backend verification failed:', error);
            return false;
        }
    }
}
