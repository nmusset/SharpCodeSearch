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
        string? file = null;
        string? workspace = null;
        string? projectFilter = null;
        string? fileFilter = null;
        string? folderFilter = null;
        string output = "json";
        int maxParallelism = Environment.ProcessorCount;

        // Simple CLI argument parsing
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--pattern" when i + 1 < args.Length:
                    pattern = args[++i];
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
