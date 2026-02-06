using System.Text.Json;

using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Caching;
using SharpCodeSearch.Services;
using SharpCodeSearch.Workspace;

namespace SharpCodeSearch;

class Program
{
    static async Task<int> Main(string[] args)
    {
        string? pattern = null;
        string? replace = null;
        string? file = null;
        string? workspace = null;
        string? projectFilter = null;
        string? fileFilter = null;
        string? folderFilter = null;
        string output = "json";
        bool apply = false;
        int maxParallelism = Environment.ProcessorCount;

        // Simple CLI argument parsing
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--pattern" when i + 1 < args.Length:
                    pattern = args[++i];
                    break;
                case "--replace" when i + 1 < args.Length:
                    replace = args[++i];
                    break;
                case "--apply":
                    apply = true;
                    break;
                case "--file" when i + 1 < args.Length:
                    file = args[++i];
                    break;
                case "--workspace" when i + 1 < args.Length:
                    workspace = args[++i];
                    break;
                case "--project-filter" when i + 1 < args.Length:
                    projectFilter = args[++i];
                    break;
                case "--file-filter" when i + 1 < args.Length:
                    fileFilter = args[++i];
                    break;
                case "--folder-filter" when i + 1 < args.Length:
                    folderFilter = args[++i];
                    break;
                case "--max-parallelism" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var parallelism))
                        maxParallelism = parallelism;
                    break;
                case "--output" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;
            }
        }

        if (pattern == null)
        {
            Console.Error.WriteLine("Error: --pattern is required");
            PrintHelp();
            return 1;
        }

        try
        {
            if (replace != null)
            {
                // Execute search and replace
                var replacements = await ExecuteReplaceAsync(pattern, replace, file, workspace,
                    projectFilter, fileFilter, folderFilter, maxParallelism, apply);

                // Apply replacements to files if requested
                List<ReplacementApplicationResult>? applicationResults = null;
                if (apply && replacements.Any())
                {
                    var applier = new ReplacementApplier();
                    applicationResults = applier.ApplyReplacements(
                        replacements
                            .Select(r => new Models.ReplacementResult
                            {
                                ReplacementText = r.ReplacementCode,
                                OriginalText = r.OriginalCode,
                                FilePath = r.FilePath,
                                StartPosition = r.StartPosition,
                                EndPosition = r.EndPosition,
                                BaseIndentation = 0
                            })
                            .ToList()
                    );
                }

                // Output replacement results
                if (apply && applicationResults != null)
                {
                    OutputApplicationResults(applicationResults);
                }
                else if (output.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    OutputReplacementJson(replacements);
                }
                else
                {
                    OutputReplacementText(replacements);
                }
            }
            else
            {
                // Execute the search
                var results = await ExecuteSearchAsync(pattern, file, workspace,
                    projectFilter, fileFilter, folderFilter, maxParallelism);

                // Output results
                if (output.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    OutputJson(results);
                }
                else
                {
                    OutputText(results);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    static async Task<List<SearchResult>> ExecuteSearchAsync(
        string pattern,
        string? file,
        string? workspace,
        string? projectFilter,
        string? fileFilter,
        string? folderFilter,
        int maxParallelism)
    {
        // Parse the pattern
        var parser = new PatternParser();
        var patternAst = parser.Parse(pattern);

        var results = new List<SearchResult>();

        if (file != null)
        {
            // Search in a single file
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"File not found: {file}");
            }

            var code = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(code, path: file);
            var root = syntaxTree.GetRoot();

            var matcher = new PatternMatcher();
            var matches = matcher.FindMatches(patternAst, root);

            foreach (var match in matches)
            {
                var lineSpan = match.Location.GetLineSpan();
                results.Add(new SearchResult
                {
                    FilePath = file,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    MatchedCode = match.Node.ToString(),
                    Placeholders = match.Placeholders
                });
            }
        }
        else
        {
            // Workspace-level search
            var workspacePath = workspace ?? Directory.GetCurrentDirectory();

            var compilationManager = new CompilationManager();
            var progressReporter = new JsonProgressReporter();
            var workspaceMatcher = new WorkspaceMatcher(compilationManager, progressReporter);

            var options = new WorkspaceSearchOptions
            {
                ProjectFilter = projectFilter,
                FileFilter = fileFilter,
                FolderFilter = folderFilter,
                MaxDegreeOfParallelism = maxParallelism
            };

            var searchResult = await workspaceMatcher.SearchWorkspaceAsync(
                patternAst,
                workspacePath,
                options);

            foreach (var match in searchResult.Matches)
            {
                var lineSpan = match.Location.GetLineSpan();
                results.Add(new SearchResult
                {
                    FilePath = match.FilePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    MatchedCode = match.Node.ToString(),
                    Placeholders = match.Placeholders
                });
            }

            // Report any errors
            if (searchResult.Errors.Any())
            {
                Console.Error.WriteLine($"Encountered {searchResult.Errors.Count} error(s) during search:");
                foreach (var error in searchResult.Errors)
                {
                    Console.Error.WriteLine($"  [{error.ErrorType}] {error.FilePath}: {error.Message}");
                }
            }
        }

        return results;
    }

    static async Task<List<ReplacementOutput>> ExecuteReplaceAsync(
        string pattern,
        string replacePattern,
        string? file,
        string? workspace,
        string? projectFilter,
        string? fileFilter,
        string? folderFilter,
        int maxParallelism,
        bool apply = false)
    {
        // Parse patterns
        var parser = new PatternParser();
        var searchPatternAst = parser.Parse(pattern);
        var replacePatternAst = parser.ParseReplacePattern(replacePattern, searchPatternAst);

        var replacements = new List<ReplacementOutput>();

        if (file != null)
        {
            // Replace in a single file
            if (!File.Exists(file))
            {
                throw new FileNotFoundException($"File not found: {file}");
            }

            var code = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(code, path: file);
            var root = syntaxTree.GetRoot();

            var matcher = new PatternMatcher();
            var matches = matcher.FindMatches(searchPatternAst, root);

            foreach (var match in matches)
            {
                var result = match.ApplyReplacement(replacePatternAst);
                var lineSpan = match.Location.GetLineSpan();
                var span = match.Location.SourceSpan;

                replacements.Add(new ReplacementOutput
                {
                    FilePath = file,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    StartPosition = span.Start,
                    EndPosition = span.End,
                    OriginalCode = result.OriginalText,
                    ReplacementCode = result.ReplacementText,
                    Placeholders = match.Placeholders
                });
            }
        }
        else
        {
            // Workspace-level replace
            var workspacePath = workspace ?? Directory.GetCurrentDirectory();

            var compilationManager = new CompilationManager();
            var progressReporter = new JsonProgressReporter();
            var workspaceMatcher = new WorkspaceMatcher(compilationManager, progressReporter);

            var options = new WorkspaceSearchOptions
            {
                ProjectFilter = projectFilter,
                FileFilter = fileFilter,
                FolderFilter = folderFilter,
                MaxDegreeOfParallelism = maxParallelism
            };

            var searchResult = await workspaceMatcher.SearchWorkspaceAsync(
                searchPatternAst,
                workspacePath,
                options);

            foreach (var matchResult in searchResult.Matches)
            {
                // Create PatternMatch from MatchResult
                var patternMatch = new PatternMatch
                {
                    Node = matchResult.Node,
                    Location = matchResult.Location,
                    Placeholders = matchResult.Placeholders
                };

                var result = patternMatch.ApplyReplacement(replacePatternAst);
                var lineSpan = matchResult.Location.GetLineSpan();
                var span = matchResult.Location.SourceSpan;

                replacements.Add(new ReplacementOutput
                {
                    FilePath = matchResult.FilePath,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    StartPosition = span.Start,
                    EndPosition = span.End,
                    OriginalCode = result.OriginalText,
                    ReplacementCode = result.ReplacementText,
                    Placeholders = matchResult.Placeholders
                });
            }

            // Report any errors
            if (searchResult.Errors.Any())
            {
                Console.Error.WriteLine($"Encountered {searchResult.Errors.Count} error(s) during search:");
                foreach (var error in searchResult.Errors)
                {
                    Console.Error.WriteLine($"  [{error.ErrorType}] {error.FilePath}: {error.Message}");
                }
            }
        }

        return replacements;
    }

    static void OutputReplacementJson(List<ReplacementOutput> replacements)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(new
        {
            replacementCount = replacements.Count,
            replacements = replacements
        }, options);

        Console.WriteLine(json);
    }

    static void OutputReplacementText(List<ReplacementOutput> replacements)
    {
        Console.WriteLine($"Found {replacements.Count} replacement(s):");
        Console.WriteLine();

        foreach (var replacement in replacements)
        {
            Console.WriteLine($"{replacement.FilePath}:{replacement.Line}:{replacement.Column}");
            Console.WriteLine("  Original:");
            Console.WriteLine($"    {replacement.OriginalCode}");
            Console.WriteLine("  Replacement:");
            Console.WriteLine($"    {replacement.ReplacementCode}");
            if (replacement.Placeholders.Any())
            {
                Console.WriteLine("  Placeholders:");
                foreach (var kvp in replacement.Placeholders)
                {
                    Console.WriteLine($"    {kvp.Key} = {kvp.Value}");
                }
            }
            Console.WriteLine();
        }
    }

    static void OutputApplicationResults(List<ReplacementApplicationResult> results)
    {
        var totalApplied = results.Sum(r => r.ReplacementsApplied);
        var totalErrors = results.Count(r => !r.Success);

        Console.WriteLine($"Applied {totalApplied} replacement(s) to {results.Count(r => r.Success)} file(s)");
        if (totalErrors > 0)
        {
            Console.WriteLine($"Encountered {totalErrors} error(s):");
        }
        Console.WriteLine();

        foreach (var result in results)
        {
            if (result.Success)
            {
                Console.WriteLine($"✓ {result.FilePath}: {result.ReplacementsApplied} replacement(s) applied");
            }
            else
            {
                Console.WriteLine($"✗ {result.FilePath}: {result.Error}");
            }
        }
    }

    static void OutputJson(List<SearchResult> results)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(new
        {
            matchCount = results.Count,
            matches = results
        }, options);

        Console.WriteLine(json);
    }

    static void OutputText(List<SearchResult> results)
    {
        Console.WriteLine($"Found {results.Count} match(es):");
        Console.WriteLine();

        foreach (var result in results)
        {
            Console.WriteLine($"{result.FilePath}:{result.Line}:{result.Column}");
            Console.WriteLine($"  {result.MatchedCode}");
            if (result.Placeholders.Any())
            {
                Console.WriteLine("  Placeholders:");
                foreach (var kvp in result.Placeholders)
                {
                    Console.WriteLine($"    {kvp.Key} = {kvp.Value}");
                }
            }
            Console.WriteLine();
        }
    }

    static void PrintHelp()
    {
        Console.WriteLine("SharpCodeSearch - Semantic pattern matching for C# code");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  SharpCodeSearch --pattern <pattern> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --pattern <pattern>           The search pattern to match against C# code (required)");
        Console.WriteLine("  --replace <pattern>           The replacement pattern (uses captured placeholders from search)");
        Console.WriteLine("  --file <file>                 Search in a single C# file");
        Console.WriteLine("  --workspace <path>            Search entire workspace (default: current directory)");
        Console.WriteLine("  --project-filter <pattern>    Filter projects (e.g., \"*.Tests.csproj\")");
        Console.WriteLine("  --file-filter <pattern>       Filter files (e.g., \"*Controller.cs\")");
        Console.WriteLine("  --folder-filter <name>        Filter by folder path (e.g., \"Controllers\")");
        Console.WriteLine("  --max-parallelism <n>         Max parallel tasks (default: CPU count)");
        Console.WriteLine("  --output <format>             Output format: json|text (default: json)");
        Console.WriteLine("  --help, -h                    Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  # Search a single file");
        Console.WriteLine("  SharpCodeSearch --pattern \"Console.WriteLine($arg$)\" --file Program.cs");
        Console.WriteLine();
        Console.WriteLine("  # Search entire workspace");
        Console.WriteLine("  SharpCodeSearch --pattern \"$x$ + $y$\" --workspace .");
        Console.WriteLine();
        Console.WriteLine("  # Search with filters");
        Console.WriteLine("  SharpCodeSearch --pattern \"$method$($args$)\" --folder-filter \"Controllers\"");
        Console.WriteLine();
        Console.WriteLine("  # Search and replace");
        Console.WriteLine("  SharpCodeSearch --pattern \"$var$++\" --replace \"$var$ = $var$ + 1\" --file Program.cs");
        Console.WriteLine();
        Console.WriteLine("  # Replace try-catch with using statement");
        Console.WriteLine("  SharpCodeSearch --pattern \"$var$ = new $type$($args$)\" --replace \"using var $var$ = new $type$($args$)\"");
    }
}

class SearchResult
{
    public required string FilePath { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
    public required string MatchedCode { get; init; }
    public Dictionary<string, string> Placeholders { get; init; } = new();
}

class ReplacementOutput
{
    public required string FilePath { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }
    public int StartPosition { get; init; }
    public int EndPosition { get; init; }
    public required string OriginalCode { get; init; }
    public required string ReplacementCode { get; init; }
    public Dictionary<string, string> Placeholders { get; init; } = new();
}
