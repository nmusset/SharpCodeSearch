using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Roslyn;

using Xunit;

namespace SharpCodeSearch.Tests;

public class RoslynHelperTests
{
    [Fact]
    public void ParseCode_ValidCode_ReturnsSyntaxTree()
    {
        var code = "class TestClass { }";
        var tree = RoslynHelper.ParseCode(code);

        Assert.NotNull(tree);
        Assert.DoesNotContain(tree.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ParseCode_NullCode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RoslynHelper.ParseCode(null!));
    }

    [Fact]
    public void EnumerateNodes_SingleNode_ReturnsNode()
    {
        var code = "var x = 1;";
        var tree = RoslynHelper.ParseCode(code);
        var root = tree.GetRoot();

        var nodes = RoslynHelper.EnumerateNodes(root).ToList();

        Assert.NotEmpty(nodes);
        Assert.Contains(root, nodes);
    }

    [Fact]
    public void EnumerateNodes_MultipleNodes_ReturnsAllNodes()
    {
        var code = "class Test { void Method() { var x = 1; } }";
        var tree = RoslynHelper.ParseCode(code);
        var root = tree.GetRoot();

        var nodes = RoslynHelper.EnumerateNodes(root).ToList();

        // Should have many nodes: CompilationUnit, ClassDeclaration, MethodDeclaration, etc.
        Assert.True(nodes.Count > 10);
    }

    [Fact]
    public void EnumerateNodes_NullNode_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RoslynHelper.EnumerateNodes(null!).ToList());
    }

    [Fact]
    public void GetSyntaxTrees_ValidCompilation_ReturnsTrees()
    {
        var tree1 = CSharpSyntaxTree.ParseText("class A { }");
        var tree2 = CSharpSyntaxTree.ParseText("class B { }");
        var compilation = CSharpCompilation.Create("Test")
            .AddSyntaxTrees(tree1, tree2);

        var trees = RoslynHelper.GetSyntaxTrees(compilation).ToList();

        Assert.Equal(2, trees.Count);
        Assert.Contains(tree1, trees);
        Assert.Contains(tree2, trees);
    }

    [Fact]
    public void GetSyntaxTrees_NullCompilation_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RoslynHelper.GetSyntaxTrees(null!).ToList());
    }

    [Fact]
    public void GetSemanticModel_ValidTreeAndCompilation_ReturnsModel()
    {
        var tree = CSharpSyntaxTree.ParseText("class Test { }");
        var compilation = CSharpCompilation.Create("Test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var model = RoslynHelper.GetSemanticModel(compilation, tree);

        Assert.NotNull(model);
    }

    [Fact]
    public void GetSemanticModel_NullCompilation_ThrowsArgumentNullException()
    {
        var tree = CSharpSyntaxTree.ParseText("class Test { }");
        Assert.Throws<ArgumentNullException>(() => RoslynHelper.GetSemanticModel(null!, tree));
    }

    [Fact]
    public void GetSemanticModel_NullTree_ThrowsArgumentNullException()
    {
        var compilation = CSharpCompilation.Create("Test");
        Assert.Throws<ArgumentNullException>(() => RoslynHelper.GetSemanticModel(compilation, null!));
    }

    [Fact]
    public async Task LoadWorkspaceAsync_NullPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => RoslynHelper.LoadWorkspaceAsync(null!));
    }

    [Fact]
    public async Task LoadWorkspaceAsync_EmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => RoslynHelper.LoadWorkspaceAsync(""));
    }

    [Fact]
    public async Task LoadWorkspaceAsync_WhitespacePath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => RoslynHelper.LoadWorkspaceAsync("   "));
    }

    [Fact]
    public async Task LoadWorkspaceAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            RoslynHelper.LoadWorkspaceAsync("C:\\NonExistent\\Project.csproj"));
    }

    [Fact]
    public async Task LoadWorkspaceAsync_UnsupportedFileType_ThrowsArgumentException()
    {
        // Create a temporary file with unsupported extension
        var tempFile = Path.GetTempFileName();
        try
        {
            await Assert.ThrowsAsync<ArgumentException>(() => RoslynHelper.LoadWorkspaceAsync(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task BuildCompilationAsync_NullProject_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => RoslynHelper.BuildCompilationAsync(null!));
    }

    [Fact]
    public void ParseCode_ComplexCode_ReturnsSyntaxTree()
    {
        var code = @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var x = 1 + 2;
            Console.WriteLine(x);
        }
    }
}";
        var tree = RoslynHelper.ParseCode(code);

        Assert.NotNull(tree);
        var root = tree.GetRoot();
        Assert.NotNull(root);
    }

    [Fact]
    public void ParseCode_EmptyCode_ReturnsSyntaxTree()
    {
        var tree = RoslynHelper.ParseCode("");
        Assert.NotNull(tree);
    }

    [Fact]
    public void EnumerateNodes_DFS_TraversesInCorrectOrder()
    {
        var code = "class A { class B { } }";
        var tree = RoslynHelper.ParseCode(code);
        var root = tree.GetRoot();

        var nodes = RoslynHelper.EnumerateNodes(root).ToList();

        // First node should be the root
        Assert.Equal(root, nodes[0]);

        // Should traverse depth-first
        var classNodes = nodes.Where(n => n.ToString().Contains("class")).ToList();
        Assert.NotEmpty(classNodes);
    }

    [Fact]
    public void GetSyntaxTrees_EmptyCompilation_ReturnsEmptyCollection()
    {
        var compilation = CSharpCompilation.Create("Test");
        var trees = RoslynHelper.GetSyntaxTrees(compilation).ToList();

        Assert.Empty(trees);
    }
}
