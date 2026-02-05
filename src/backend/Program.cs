using System.Text.Json;

using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Services;

namespace SharpCodeSearch;

class Program
{
    static int Main(string[] args)
    {
        string? pattern = null;
        string? file = null;
        string output = "json";

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
            var results = ExecuteSearch(pattern, file);

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

    static List<SearchResult> ExecuteSearch(string pattern, string? file)
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
            // TODO: Implement workspace-level search
            throw new NotImplementedException("Workspace-level search not yet implemented. Please specify a file with --file");
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
        Console.WriteLine("  SharpCodeSearch --pattern <pattern> [--file <file>] [--output <format>]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --pattern <pattern>    The search pattern to match against C# code (required)");
        Console.WriteLine("  --file <file>          The C# file to search (optional, will scan workspace if not provided)");
        Console.WriteLine("  --output <format>      Output format: json|text (default: json)");
        Console.WriteLine("  --help, -h             Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SharpCodeSearch --pattern \"Console.WriteLine($arg$)\" --file Program.cs");
        Console.WriteLine("  SharpCodeSearch --pattern \"$x$ + $y$\" --file Calculator.cs --output text");
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
