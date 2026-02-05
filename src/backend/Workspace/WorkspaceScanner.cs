using System.Collections.Concurrent;

namespace SharpCodeSearch.Workspace;

/// <summary>
/// Scans a workspace for C# files and project files.
/// </summary>
public class WorkspaceScanner
{
    private readonly string _workspacePath;

    public WorkspaceScanner(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentException("Workspace path cannot be null or empty", nameof(workspacePath));

        if (!Directory.Exists(workspacePath))
            throw new DirectoryNotFoundException($"Workspace directory not found: {workspacePath}");

        _workspacePath = Path.GetFullPath(workspacePath);
    }

    /// <summary>
    /// Finds all .csproj files in the workspace.
    /// </summary>
    /// <param name="projectFilter">Optional pattern to filter projects (e.g., "*.Tests.csproj")</param>
    /// <returns>List of absolute paths to .csproj files</returns>
    public List<string> FindProjects(string? projectFilter = null)
    {
        var searchPattern = projectFilter ?? "*.csproj";
        var projects = Directory.EnumerateFiles(_workspacePath, searchPattern, SearchOption.AllDirectories)
            .Where(p => !IsInExcludedDirectory(p))
            .ToList();

        return projects;
    }

    /// <summary>
    /// Finds all .cs files in the workspace.
    /// </summary>
    /// <param name="fileFilter">Optional pattern to filter files (e.g., "*.Tests.cs")</param>
    /// <returns>List of absolute paths to .cs files</returns>
    public List<string> FindCSharpFiles(string? fileFilter = null)
    {
        var searchPattern = fileFilter ?? "*.cs";
        var files = Directory.EnumerateFiles(_workspacePath, searchPattern, SearchOption.AllDirectories)
            .Where(f => !IsInExcludedDirectory(f))
            .ToList();

        return files;
    }

    /// <summary>
    /// Finds all .cs files within a specific directory.
    /// </summary>
    /// <param name="directoryPath">The directory to search in</param>
    /// <param name="recursive">Whether to search recursively</param>
    /// <returns>List of absolute paths to .cs files</returns>
    public List<string> FindCSharpFilesInDirectory(string directoryPath, bool recursive = true)
    {
        if (!Directory.Exists(directoryPath))
            return new List<string>();

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(directoryPath, "*.cs", searchOption)
            .Where(f => !IsInExcludedDirectory(f))
            .ToList();

        return files;
    }

    /// <summary>
    /// Checks if a path is within an excluded directory (bin, obj, node_modules, etc.).
    /// </summary>
    private bool IsInExcludedDirectory(string path)
    {
        var excludedDirs = new[] { "bin", "obj", "node_modules", ".git", ".vs", "packages" };
        var pathParts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return pathParts.Any(part => excludedDirs.Contains(part, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets project statistics for the workspace.
    /// </summary>
    public WorkspaceStats GetWorkspaceStats()
    {
        var projects = FindProjects();
        var csFiles = FindCSharpFiles();

        return new WorkspaceStats
        {
            WorkspacePath = _workspacePath,
            ProjectCount = projects.Count,
            CSharpFileCount = csFiles.Count,
            Projects = projects
        };
    }
}

/// <summary>
/// Statistics about a workspace.
/// </summary>
public class WorkspaceStats
{
    public required string WorkspacePath { get; init; }
    public int ProjectCount { get; init; }
    public int CSharpFileCount { get; init; }
    public List<string> Projects { get; init; } = new();
}
