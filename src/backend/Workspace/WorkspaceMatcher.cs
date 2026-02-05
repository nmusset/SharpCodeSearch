using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Caching;
using SharpCodeSearch.Models;
using SharpCodeSearch.Services;

namespace SharpCodeSearch.Workspace;

/// <summary>
/// Matches patterns across an entire workspace with parallel processing.
/// </summary>
public class WorkspaceMatcher
{
    private readonly CompilationManager _compilationManager;
    private readonly IProgressReporter? _progressReporter;

    public WorkspaceMatcher(
        CompilationManager compilationManager,
        IProgressReporter? progressReporter = null)
    {
        _compilationManager = compilationManager ?? throw new ArgumentNullException(nameof(compilationManager));
        _progressReporter = progressReporter;
    }

    /// <summary>
    /// Searches for pattern matches across the entire workspace.
    /// </summary>
    /// <param name="pattern">The parsed pattern AST to match</param>
    /// <param name="workspacePath">Path to the workspace root</param>
    /// <param name="options">Search options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all matches found</returns>
    public async Task<WorkspaceSearchResult> SearchWorkspaceAsync(
        PatternAst pattern,
        string workspacePath,
        WorkspaceSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        var scanner = new WorkspaceScanner(workspacePath);
        var results = new ConcurrentBag<MatchResult>();
        var errors = new ConcurrentBag<WorkspaceError>();

        _progressReporter?.ReportProgress(new ProgressInfo
        {
            Stage = "scanning",
            Message = "Scanning workspace for projects..."
        });

        // Find all projects
        var projects = scanner.FindProjects(options.ProjectFilter);

        if (projects.Count == 0)
        {
            _progressReporter?.ReportProgress(new ProgressInfo
            {
                Stage = "complete",
                Message = "No projects found in workspace",
                TotalFiles = 0,
                ProcessedFiles = 0
            });

            return new WorkspaceSearchResult
            {
                Matches = new List<MatchResult>(),
                Errors = new List<WorkspaceError>(),
                TotalFilesScanned = 0,
                TotalMatchesFound = 0
            };
        }

        _progressReporter?.ReportProgress(new ProgressInfo
        {
            Stage = "loading",
            Message = $"Loading {projects.Count} project(s)..."
        });

        // Build compilations for all projects
        var compilations = new List<(string ProjectPath, Compilation Compilation, SemanticModel[] Models)>();

        foreach (var projectPath in projects)
        {
            try
            {
                var compilationResult = await _compilationManager.GetOrBuildCompilationAsync(projectPath);

                if (compilationResult.Compilation != null)
                {
                    var semanticModels = compilationResult.Compilation.SyntaxTrees
                        .Select(tree => compilationResult.Compilation.GetSemanticModel(tree))
                        .ToArray();

                    compilations.Add((projectPath, compilationResult.Compilation, semanticModels));
                }
                else
                {
                    errors.Add(new WorkspaceError
                    {
                        FilePath = projectPath,
                        ErrorType = "compilation",
                        Message = $"Failed to build compilation: {string.Join("; ", compilationResult.Errors)}"
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add(new WorkspaceError
                {
                    FilePath = projectPath,
                    ErrorType = "exception",
                    Message = ex.Message
                });
            }
        }

        if (compilations.Count == 0)
        {
            return new WorkspaceSearchResult
            {
                Matches = new List<MatchResult>(),
                Errors = errors.ToList(),
                TotalFilesScanned = 0,
                TotalMatchesFound = 0
            };
        }

        // Collect all files to process
        var filesToProcess = new List<(string FilePath, SyntaxTree Tree, SemanticModel Model)>();
        foreach (var (projectPath, compilation, models) in compilations)
        {
            var trees = compilation.SyntaxTrees.ToArray();
            for (int i = 0; i < trees.Length; i++)
            {
                var tree = trees[i];
                if (tree.FilePath != null && ShouldProcessFile(tree.FilePath, options))
                {
                    filesToProcess.Add((tree.FilePath, tree, models[i]));
                }
            }
        }

        var totalFiles = filesToProcess.Count;
        var processedFiles = 0;

        _progressReporter?.ReportProgress(new ProgressInfo
        {
            Stage = "searching",
            Message = $"Searching {totalFiles} file(s)...",
            TotalFiles = totalFiles,
            ProcessedFiles = 0
        });

        // Process files in parallel
        await Parallel.ForEachAsync(
            filesToProcess,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (fileInfo, ct) =>
            {
                try
                {
                    var (filePath, tree, model) = fileInfo;
                    var root = await tree.GetRootAsync(ct);

                    // Create a PatternMatcher with the semantic model for this file
                    var patternMatcher = new PatternMatcher(model);

                    // Find matches in this file
                    var fileMatches = patternMatcher.FindMatches(pattern, root);

                    foreach (var match in fileMatches)
                    {
                        results.Add(new MatchResult
                        {
                            FilePath = filePath,
                            Node = match.Node,
                            Location = match.Location,
                            Placeholders = match.Placeholders
                        });
                    }

                    // Report progress
                    var processed = Interlocked.Increment(ref processedFiles);
                    if (processed % 10 == 0 || processed == totalFiles)
                    {
                        _progressReporter?.ReportProgress(new ProgressInfo
                        {
                            Stage = "searching",
                            Message = $"Searching... {processed}/{totalFiles} files",
                            TotalFiles = totalFiles,
                            ProcessedFiles = processed
                        });
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new WorkspaceError
                    {
                        FilePath = fileInfo.FilePath,
                        ErrorType = "matching",
                        Message = ex.Message
                    });
                }
            });

        _progressReporter?.ReportProgress(new ProgressInfo
        {
            Stage = "complete",
            Message = $"Search complete. Found {results.Count} match(es).",
            TotalFiles = totalFiles,
            ProcessedFiles = totalFiles
        });

        // Sort and deduplicate results
        var sortedResults = results
            .OrderBy(r => r.FilePath)
            .ThenBy(r => r.Location.SourceSpan.Start)
            .ToList();

        return new WorkspaceSearchResult
        {
            Matches = sortedResults,
            Errors = errors.ToList(),
            TotalFilesScanned = totalFiles,
            TotalMatchesFound = sortedResults.Count
        };
    }

    /// <summary>
    /// Determines if a file should be processed based on the options.
    /// </summary>
    private bool ShouldProcessFile(string filePath, WorkspaceSearchOptions options)
    {
        // Apply file filter
        if (!string.IsNullOrEmpty(options.FileFilter))
        {
            var fileName = Path.GetFileName(filePath);
            if (!IsMatch(fileName, options.FileFilter))
                return false;
        }

        // Apply folder filter
        if (!string.IsNullOrEmpty(options.FolderFilter))
        {
            if (!filePath.Contains(options.FolderFilter, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Simple wildcard pattern matching.
    /// </summary>
    private bool IsMatch(string text, string pattern)
    {
        // Simple wildcard support (* and ?)
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return System.Text.RegularExpressions.Regex.IsMatch(text, regexPattern,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}

/// <summary>
/// Options for workspace search.
/// </summary>
public class WorkspaceSearchOptions
{
    /// <summary>
    /// Filter for project files (e.g., "*.Tests.csproj"). Null = all projects.
    /// </summary>
    public string? ProjectFilter { get; init; }

    /// <summary>
    /// Filter for C# files (e.g., "*.Tests.cs"). Null = all files.
    /// </summary>
    public string? FileFilter { get; init; }

    /// <summary>
    /// Filter by folder path (e.g., "Controllers"). Null = all folders.
    /// </summary>
    public string? FolderFilter { get; init; }

    /// <summary>
    /// Maximum degree of parallelism. Default is number of processors.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
}

/// <summary>
/// Result of a workspace search.
/// </summary>
public class WorkspaceSearchResult
{
    public required List<MatchResult> Matches { get; init; }
    public required List<WorkspaceError> Errors { get; init; }
    public int TotalFilesScanned { get; init; }
    public int TotalMatchesFound { get; init; }
}

/// <summary>
/// Represents a match result from pattern matching.
/// </summary>
public class MatchResult
{
    public required string FilePath { get; init; }
    public required SyntaxNode Node { get; init; }
    public required Location Location { get; init; }
    public Dictionary<string, string> Placeholders { get; init; } = new();
}

/// <summary>
/// Represents an error encountered during workspace search.
/// </summary>
public class WorkspaceError
{
    public required string FilePath { get; init; }
    public required string ErrorType { get; init; }
    public required string Message { get; init; }
}

/// <summary>
/// Interface for reporting search progress.
/// </summary>
public interface IProgressReporter
{
    void ReportProgress(ProgressInfo progress);
}

/// <summary>
/// Progress information during search.
/// </summary>
public class ProgressInfo
{
    public required string Stage { get; init; }
    public required string Message { get; init; }
    public int? TotalFiles { get; init; }
    public int? ProcessedFiles { get; init; }
}

/// <summary>
/// JSON-based progress reporter that writes to stderr.
/// </summary>
public class JsonProgressReporter : IProgressReporter
{
    public void ReportProgress(ProgressInfo progress)
    {
        var json = JsonSerializer.Serialize(new
        {
            type = "progress",
            stage = progress.Stage,
            message = progress.Message,
            totalFiles = progress.TotalFiles,
            processedFiles = progress.ProcessedFiles
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Write progress to stderr to avoid corrupting JSON output on stdout
        Console.Error.WriteLine(json);
    }
}
