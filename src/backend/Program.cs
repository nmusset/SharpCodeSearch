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

        Console.WriteLine($"Pattern: {pattern}");
        Console.WriteLine($"File: {file ?? "workspace"}");
        Console.WriteLine($"Output format: {output}");

        // TODO: Implement pattern matching logic
        Console.WriteLine("Pattern matching not yet implemented.");

        return 0;
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
    }
}
