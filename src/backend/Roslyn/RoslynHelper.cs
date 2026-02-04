using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace SharpCodeSearch.Roslyn;

/// <summary>
/// Helper class for working with Roslyn to load and analyze C# code.
/// </summary>
public class RoslynHelper
{
    /// <summary>
    /// Loads a workspace from a .csproj or .sln file.
    /// </summary>
    /// <param name="workspacePath">Path to .csproj or .sln file</param>
    /// <returns>MSBuild workspace</returns>
    public static async Task<MSBuildWorkspace> LoadWorkspaceAsync(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("Workspace path cannot be null or empty", nameof(workspacePath));

        if (!File.Exists(workspacePath))
            throw new FileNotFoundException($"Workspace file not found: {workspacePath}");

        var workspace = MSBuildWorkspace.Create();

        // Register for diagnostics
        workspace.RegisterWorkspaceFailedHandler((args) =>
        {
            Console.Error.WriteLine($"Workspace error: {args.Diagnostic.Message}");
        });

        if (workspacePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
        {
            await workspace.OpenSolutionAsync(workspacePath);
        }
        else if (workspacePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            await workspace.OpenProjectAsync(workspacePath);
        }
        else
        {
            throw new ArgumentException($"Unsupported workspace file type: {workspacePath}. Expected .sln or .csproj");
        }

        return workspace;
    }

    /// <summary>
    /// Builds a compilation from a project.
    /// </summary>
    /// <param name="project">The project to compile</param>
    /// <returns>Compilation object</returns>
    public static async Task<Compilation?> BuildCompilationAsync(Project project)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        var compilation = await project.GetCompilationAsync();

        if (compilation == null)
        {
            Console.Error.WriteLine($"Failed to build compilation for project: {project.Name}");
            return null;
        }

        // Report compilation errors
        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error);

        foreach (var diagnostic in diagnostics)
        {
            Console.Error.WriteLine($"Compilation error: {diagnostic}");
        }

        return compilation;
    }

    /// <summary>
    /// Gets all syntax trees from a compilation.
    /// </summary>
    /// <param name="compilation">The compilation</param>
    /// <returns>Collection of syntax trees</returns>
    public static IEnumerable<SyntaxTree> GetSyntaxTrees(Compilation compilation)
    {
        if (compilation == null)
            throw new ArgumentNullException(nameof(compilation));

        return compilation.SyntaxTrees;
    }

    /// <summary>
    /// Enumerates all syntax nodes in a tree using depth-first search.
    /// </summary>
    /// <param name="node">The root node to start from</param>
    /// <returns>All descendant nodes</returns>
    public static IEnumerable<SyntaxNode> EnumerateNodes(SyntaxNode node)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));

        // Return current node
        yield return node;

        // Recursively enumerate all children
        foreach (var child in node.ChildNodes())
        {
            foreach (var descendant in EnumerateNodes(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Gets the semantic model for a syntax tree.
    /// </summary>
    /// <param name="compilation">The compilation</param>
    /// <param name="tree">The syntax tree</param>
    /// <returns>Semantic model</returns>
    public static SemanticModel GetSemanticModel(Compilation compilation, SyntaxTree tree)
    {
        if (compilation == null)
            throw new ArgumentNullException(nameof(compilation));
        if (tree == null)
            throw new ArgumentNullException(nameof(tree));

        return compilation.GetSemanticModel(tree);
    }

    /// <summary>
    /// Parses C# code into a syntax tree.
    /// </summary>
    /// <param name="code">C# source code</param>
    /// <returns>Syntax tree</returns>
    public static SyntaxTree ParseCode(string code)
    {
        if (code == null)
            throw new ArgumentNullException(nameof(code));

        return CSharpSyntaxTree.ParseText(code);
    }
}
