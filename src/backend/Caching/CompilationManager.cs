using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SharpCodeSearch.Caching;

/// <summary>
/// Manages compilation caching and tracks file modifications.
/// </summary>
public class CompilationManager
{
    private readonly ConcurrentDictionary<string, CachedCompilation> _compilationCache;
    private readonly ConcurrentDictionary<string, DateTime> _fileTimestamps;

    public CompilationManager()
    {
        _compilationCache = new ConcurrentDictionary<string, CachedCompilation>();
        _fileTimestamps = new ConcurrentDictionary<string, DateTime>();
    }

    /// <summary>
    /// Gets or builds a compilation for the specified project.
    /// </summary>
    /// <param name="projectPath">Path to the .csproj file</param>
    /// <param name="forceRebuild">Force rebuild even if cached</param>
    /// <returns>Compilation and any errors</returns>
    public async Task<CompilationResult> GetOrBuildCompilationAsync(string projectPath, bool forceRebuild = false)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
            throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));

        var normalizedPath = Path.GetFullPath(projectPath);

        // Check if we have a cached compilation and it's still valid
        if (!forceRebuild && _compilationCache.TryGetValue(normalizedPath, out var cached))
        {
            if (!HasProjectChanged(normalizedPath, cached.Timestamp))
            {
                return new CompilationResult
                {
                    Compilation = cached.Compilation,
                    IsFromCache = true,
                    Errors = cached.Errors
                };
            }
        }

        // Build new compilation
        return await BuildAndCacheCompilationAsync(normalizedPath);
    }

    /// <summary>
    /// Builds and caches a new compilation.
    /// </summary>
    private async Task<CompilationResult> BuildAndCacheCompilationAsync(string projectPath)
    {
        var errors = new List<string>();

        try
        {
            var workspace = MSBuildWorkspace.Create();
            workspace.RegisterWorkspaceFailedHandler((args) =>
            {
                errors.Add($"Workspace: {args.Diagnostic.Message}");
            });

            var project = await workspace.OpenProjectAsync(projectPath);
            var compilation = await project.GetCompilationAsync();

            if (compilation == null)
            {
                errors.Add($"Failed to create compilation for {projectPath}");
                return new CompilationResult { Compilation = null, IsFromCache = false, Errors = errors };
            }

            // Collect compilation errors
            var diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error);

            foreach (var diagnostic in diagnostics)
            {
                errors.Add($"{diagnostic.Location}: {diagnostic.GetMessage()}");
            }

            // Cache the compilation
            var cached = new CachedCompilation
            {
                Compilation = compilation,
                Timestamp = DateTime.UtcNow,
                Errors = errors
            };

            _compilationCache[projectPath] = cached;
            UpdateFileTimestamps(project);

            return new CompilationResult
            {
                Compilation = compilation,
                IsFromCache = false,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            errors.Add($"Exception building compilation: {ex.Message}");
            return new CompilationResult { Compilation = null, IsFromCache = false, Errors = errors };
        }
    }

    /// <summary>
    /// Checks if the project or its files have changed since the given timestamp.
    /// </summary>
    private bool HasProjectChanged(string projectPath, DateTime cachedTimestamp)
    {
        // Check project file itself
        var projectFileInfo = new FileInfo(projectPath);
        if (!projectFileInfo.Exists || projectFileInfo.LastWriteTimeUtc > cachedTimestamp)
        {
            return true;
        }

        // Check if any source files have been modified
        // For now, just check the project file. In a full implementation,
        // we would check all source files referenced by the project.
        return false;
    }

    /// <summary>
    /// Updates file timestamps for change detection.
    /// </summary>
    private void UpdateFileTimestamps(Project project)
    {
        foreach (var document in project.Documents)
        {
            if (document.FilePath != null && File.Exists(document.FilePath))
            {
                var fileInfo = new FileInfo(document.FilePath);
                _fileTimestamps[document.FilePath] = fileInfo.LastWriteTimeUtc;
            }
        }
    }

    /// <summary>
    /// Clears all cached compilations.
    /// </summary>
    public void ClearCache()
    {
        _compilationCache.Clear();
        _fileTimestamps.Clear();
    }

    /// <summary>
    /// Invalidates the cache for a specific project.
    /// </summary>
    public void InvalidateProject(string projectPath)
    {
        var normalizedPath = Path.GetFullPath(projectPath);
        _compilationCache.TryRemove(normalizedPath, out _);
    }
}

/// <summary>
/// Represents a cached compilation.
/// </summary>
internal class CachedCompilation
{
    public required Compilation Compilation { get; init; }
    public DateTime Timestamp { get; init; }
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Result of a compilation operation.
/// </summary>
public class CompilationResult
{
    public Compilation? Compilation { get; init; }
    public bool IsFromCache { get; init; }
    public List<string> Errors { get; init; } = new();
    public bool HasErrors => Errors.Count > 0;
}
