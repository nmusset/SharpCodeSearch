using Xunit;

using SharpCodeSearch.Caching;
using SharpCodeSearch.Services;
using SharpCodeSearch.Workspace;
using SharpCodeSearch.Models;

namespace SharpCodeSearch.Tests;

public class WorkspaceIntegrationTests
{
    [Fact]
    public async Task WorkspaceScanner_FindsProjects()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var scanner = new WorkspaceScanner(workspacePath);

        // Act
        var projects = scanner.FindProjects();

        // Assert
        Assert.NotEmpty(projects);
        Assert.All(projects, p => Assert.EndsWith(".csproj", p));
    }

    [Fact]
    public async Task WorkspaceScanner_FindsCSharpFiles()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var scanner = new WorkspaceScanner(workspacePath);

        // Act
        var files = scanner.FindCSharpFiles();

        // Assert
        Assert.NotEmpty(files);
        Assert.All(files, f => Assert.EndsWith(".cs", f));
    }

    [Fact]
    public async Task WorkspaceScanner_FiltersTestFiles()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var scanner = new WorkspaceScanner(workspacePath);

        // Act
        var files = scanner.FindCSharpFiles("*Tests.cs");

        // Assert
        Assert.NotEmpty(files);
        Assert.All(files, f => Assert.Contains("Tests.cs", f));
    }

    [Fact]
    public async Task WorkspaceScanner_ExcludesBinObjDirectories()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var scanner = new WorkspaceScanner(workspacePath);

        // Act
        var files = scanner.FindCSharpFiles();

        // Assert
        Assert.DoesNotContain(files, f => f.Contains("\\bin\\") || f.Contains("\\obj\\"));
    }

    [Fact]
    public async Task CompilationManager_BuildsSimpleCompilation()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var scanner = new WorkspaceScanner(workspacePath);
        var projects = scanner.FindProjects();
        var backendProject = projects.FirstOrDefault(p => p.Contains("SharpCodeSearch.csproj") && !p.Contains("Tests"));

        Assert.NotNull(backendProject);

        var manager = new CompilationManager();

        // Act
        var result = await manager.GetOrBuildCompilationAsync(backendProject);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Compilation);
        Assert.False(result.IsFromCache);
    }

    [Fact]
    public async Task CompilationManager_UsesCache()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var scanner = new WorkspaceScanner(workspacePath);
        var projects = scanner.FindProjects();
        var backendProject = projects.FirstOrDefault(p => p.Contains("SharpCodeSearch.csproj") && !p.Contains("Tests"));

        Assert.NotNull(backendProject);

        var manager = new CompilationManager();

        // Act
        var result1 = await manager.GetOrBuildCompilationAsync(backendProject);
        var result2 = await manager.GetOrBuildCompilationAsync(backendProject);

        // Assert
        Assert.False(result1.IsFromCache);
        Assert.True(result2.IsFromCache);
        Assert.Same(result1.Compilation, result2.Compilation);
    }

    [Fact]
    public async Task WorkspaceMatcher_SearchesEntireWorkspace()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var manager = new CompilationManager();
        var matcher = new WorkspaceMatcher(manager);

        var parser = new PatternParser();
        var pattern = parser.Parse("Console.WriteLine($arg$)");

        var options = new WorkspaceSearchOptions();

        // Act
        var result = await matcher.SearchWorkspaceAsync(pattern, workspacePath, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Matches);
        Assert.True(result.TotalFilesScanned > 0);
        Assert.Equal(result.Matches.Count, result.TotalMatchesFound);
    }

    [Fact]
    public async Task WorkspaceMatcher_FiltersFiles()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var manager = new CompilationManager();
        var matcher = new WorkspaceMatcher(manager);

        var parser = new PatternParser();
        var pattern = parser.Parse("$type$ $name$");

        var options = new WorkspaceSearchOptions
        {
            FileFilter = "*Tests.cs"
        };

        // Act
        var result = await matcher.SearchWorkspaceAsync(pattern, workspacePath, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Matches);
        Assert.All(result.Matches, m => Assert.Contains("Tests.cs", m.FilePath));
    }

    [Fact]
    public async Task WorkspaceMatcher_ReportsProgress()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var manager = new CompilationManager();
        var progressReports = new List<ProgressInfo>();
        var progressReporter = new TestProgressReporter(progressReports);
        var matcher = new WorkspaceMatcher(manager, progressReporter);

        var parser = new PatternParser();
        var pattern = parser.Parse("$var$");

        var options = new WorkspaceSearchOptions();

        // Act
        var result = await matcher.SearchWorkspaceAsync(pattern, workspacePath, options);

        // Assert
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p.Stage == "scanning");
        Assert.Contains(progressReports, p => p.Stage == "loading");
        Assert.Contains(progressReports, p => p.Stage == "searching");
        Assert.Contains(progressReports, p => p.Stage == "complete");
    }

    [Fact]
    public async Task WorkspaceMatcher_HandlesEmptyWorkspace()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var manager = new CompilationManager();
            var matcher = new WorkspaceMatcher(manager);

            var parser = new PatternParser();
            var pattern = parser.Parse("$var$");

            var options = new WorkspaceSearchOptions();

            // Act
            var result = await matcher.SearchWorkspaceAsync(pattern, tempDir, options);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Matches);
            Assert.Equal(0, result.TotalFilesScanned);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task WorkspaceMatcher_ParallelProcessing()
    {
        // Arrange
        var workspacePath = GetWorkspaceRoot();
        var manager = new CompilationManager();
        var matcher = new WorkspaceMatcher(manager);

        var parser = new PatternParser();
        var pattern = parser.Parse("$var$");

        var options1 = new WorkspaceSearchOptions { MaxDegreeOfParallelism = 1 };
        var options8 = new WorkspaceSearchOptions { MaxDegreeOfParallelism = 8 };

        // Act
        var startTime1 = DateTime.Now;
        var result1 = await matcher.SearchWorkspaceAsync(pattern, workspacePath, options1);
        var duration1 = DateTime.Now - startTime1;

        var startTime8 = DateTime.Now;
        var result8 = await matcher.SearchWorkspaceAsync(pattern, workspacePath, options8);
        var duration8 = DateTime.Now - startTime8;

        // Assert
        // Both should find the same matches
        Assert.Equal(result1.TotalMatchesFound, result8.TotalMatchesFound);

        // Parallel processing should be faster (though not guaranteed on every machine)
        // Just verify it completes successfully
        Assert.True(duration8.TotalSeconds < 60);
    }

    private string GetWorkspaceRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Navigate up to find the workspace root (contains src/ and Docs/)
        while (!string.IsNullOrEmpty(currentDir) && currentDir != Path.GetPathRoot(currentDir))
        {
            if (Directory.Exists(Path.Combine(currentDir, "src")) &&
                Directory.Exists(Path.Combine(currentDir, "Docs")))
            {
                return currentDir;
            }
            currentDir = Directory.GetParent(currentDir)?.FullName ?? "";
        }

        throw new DirectoryNotFoundException("Could not find workspace root");
    }

    private class TestProgressReporter : IProgressReporter
    {
        private readonly List<ProgressInfo> _reports;

        public TestProgressReporter(List<ProgressInfo> reports)
        {
            _reports = reports;
        }

        public void ReportProgress(ProgressInfo progress)
        {
            _reports.Add(progress);
        }
    }
}
