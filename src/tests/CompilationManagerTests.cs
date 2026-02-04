using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Caching;

using Xunit;

namespace SharpCodeSearch.Tests;

public class CompilationManagerTests
{
    [Fact]
    public void Constructor_Initializes_Successfully()
    {
        var manager = new CompilationManager();
        Assert.NotNull(manager);
    }

    [Fact]
    public async Task GetOrBuildCompilationAsync_NullPath_ThrowsArgumentException()
    {
        var manager = new CompilationManager();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.GetOrBuildCompilationAsync(null!));
    }

    [Fact]
    public async Task GetOrBuildCompilationAsync_EmptyPath_ThrowsArgumentException()
    {
        var manager = new CompilationManager();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.GetOrBuildCompilationAsync(""));
    }

    [Fact]
    public async Task GetOrBuildCompilationAsync_WhitespacePath_ThrowsArgumentException()
    {
        var manager = new CompilationManager();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            manager.GetOrBuildCompilationAsync("   "));
    }

    [Fact]
    public async Task GetOrBuildCompilationAsync_NonExistentFile_ReturnsError()
    {
        var manager = new CompilationManager();
        var result = await manager.GetOrBuildCompilationAsync("C:\\NonExistent\\Project.csproj");

        Assert.False(result.IsFromCache);
        Assert.NotEmpty(result.Errors);
        Assert.Null(result.Compilation);
    }

    [Fact]
    public void ClearCache_DoesNotThrow()
    {
        var manager = new CompilationManager();
        manager.ClearCache();
        // Should not throw
    }

    [Fact]
    public void InvalidateProject_WithPath_DoesNotThrow()
    {
        var manager = new CompilationManager();
        manager.InvalidateProject("SomePath.csproj");
        // Should not throw
    }

    [Fact]
    public void CompilationResult_HasErrors_ReturnsTrueWhenErrorsExist()
    {
        var result = new CompilationResult
        {
            Errors = new List<string> { "Error 1", "Error 2" }
        };

        Assert.True(result.HasErrors);
    }

    [Fact]
    public void CompilationResult_HasErrors_ReturnsFalseWhenNoErrors()
    {
        var result = new CompilationResult
        {
            Errors = new List<string>()
        };

        Assert.False(result.HasErrors);
    }

    [Fact]
    public void CompilationResult_DefaultConstructor_InitializesEmptyErrors()
    {
        var result = new CompilationResult();

        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
        Assert.False(result.HasErrors);
    }
}
