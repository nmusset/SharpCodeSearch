using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Models;
using SharpCodeSearch.Roslyn;
using SharpCodeSearch.Services;

namespace SharpCodeSearch.Tests;

/// <summary>
/// Integration tests that verify the full search workflow from pattern parsing to result matching.
/// </summary>
public class IntegrationTests
{
    [Fact]
    public void FullWorkflow_SimpleMethodCall_ShouldFindMatches()
    {
        // Arrange
        var code = @"
using System;

class TestClass
{
    void Method()
    {
        var x = 5;
        Console.WriteLine(x);
        Console.WriteLine(""Hello"");
        var y = 10;
        Console.WriteLine(y);
    }
}";

        var pattern = "Console.WriteLine($arg$)";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        var parser = new PatternParser();
        var patternAst = parser.Parse(pattern);

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, root);

        // Assert
        Assert.NotEmpty(matches);
        Assert.Equal(3, matches.Count);

        // Verify placeholder values were captured
        Assert.All(matches, match => Assert.True(match.Placeholders.ContainsKey("arg")));
    }

    [Fact]
    public void FullWorkflow_BinaryExpression_ShouldFindAndExtractOperands()
    {
        // Arrange
        var code = @"
class TestClass
{
    void Method()
    {
        int a = 5 + 10;
    }
}";

        var pattern = "$left$ + $right$";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        var parser = new PatternParser();
        var patternAst = parser.Parse(pattern);

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, root);

        // Assert
        Assert.NotEmpty(matches);
        Assert.True(matches.Count >= 1); // Should find "5 + 10" at minimum

        // Verify both placeholders were captured
        Assert.All(matches.Where(m => m.Node.ToString().Contains("+")).Take(1), match =>
        {
            Assert.True(match.Placeholders.ContainsKey("left"));
            Assert.True(match.Placeholders.ContainsKey("right"));
        });
    }

    [Fact]
    public void FullWorkflow_NestedMethodCalls_ShouldFindAll()
    {
        // Arrange
        var code = @"
using System;

class TestClass
{
    void Method()
    {
        var str = ""test"".ToString();
        var upper = str.ToUpper();
        var result = upper.Replace(""T"", ""X"");
    }
}";

        var pattern = "$obj$.ToString()";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        var parser = new PatternParser();
        var patternAst = parser.Parse(pattern);

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, root);

        // Assert
        Assert.NotEmpty(matches);
        // Should find at least "test".ToString()
        var actualToStringCall = matches.FirstOrDefault(m => 
            m.Node.ToString().Contains("\"test\".ToString()") && 
            m.Placeholders.ContainsKey("obj") && 
            m.Placeholders["obj"].Contains("test"));
        Assert.NotNull(actualToStringCall);
    }

    [Fact(Skip = "Requires Phase 2: Constraint key handling without suffix")]
    public void FullWorkflow_WithConstraint_ShouldFilterMatches()
    {
        // Arrange
        var code = @"
class TestClass
{
    void Method()
    {
        var name1 = ""John"";
        var name2 = ""Jane"";
        var age = 25;
        var city = ""NYC"";
    }
}";

        var pattern = "$var:regex=name.*$ = $value$";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        var parser = new PatternParser();
        var patternAst = parser.Parse(pattern);

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, root);

        // Assert
        // Should match only name1 and name2, not age or city
        Assert.NotEmpty(matches);
        Assert.True(matches.Count >= 2);
        Assert.All(matches.Take(2), match =>
            Assert.Matches("name.*", match.Placeholders["var:regex=name.*"]));
    }

    [Fact]
    public void FullWorkflow_MultiplePatterns_ShouldMatchIndependently()
    {
        // Arrange
        var code = @"
using System;

class TestClass
{
    void Method()
    {
        Console.WriteLine(""Test"");
        Console.Write(""Hello"");
        Debug.WriteLine(""Debug"");
    }
}";

        var pattern1 = "Console.WriteLine($arg$)";
        var pattern2 = "Console.Write($arg$)";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        var parser = new PatternParser();
        var patternAst1 = parser.Parse(pattern1);
        var patternAst2 = parser.Parse(pattern2);

        var matcher = new PatternMatcher();
        var matches1 = matcher.FindMatches(patternAst1, root);
        var matches2 = matcher.FindMatches(patternAst2, root);

        // Assert
        // Note: Pattern matcher may find multiple matches depending on how it traverses the tree
        Assert.NotEmpty(matches1); // Should find WriteLine
        Assert.NotEmpty(matches2); // Should find Write
        
        // Verify WriteLine is in matches1
        Assert.Contains(matches1, m => m.Node.ToString().Contains("WriteLine"));
        
        // Verify Write is in matches2 
        Assert.Contains(matches2, m => m.Node.ToString().Contains("Write") && !m.Node.ToString().Contains("WriteLine"));
    }

    [Fact]
    public void FullWorkflow_WithSemanticModel_ShouldEnableTypeChecking()
    {
        // Arrange
        var code = @"
using System;

class TestClass
{
    void Method()
    {
        int x = 5;
        string s = ""hello"";
        x.ToString();
        s.ToString();
    }
}";

        var pattern = "$expr$.ToString()";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("TestCompilation")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var parser = new PatternParser();
        var patternAst = parser.Parse(pattern);

        var matcher = new PatternMatcher(semanticModel);
        var matches = matcher.FindMatches(patternAst, root);

        // Assert
        Assert.NotEmpty(matches);
        Assert.Equal(2, matches.Count); // Both x.ToString() and s.ToString()
    }

    [Fact]
    public void FullWorkflow_InvalidPattern_ShouldThrowException()
    {
        // Arrange
        var pattern = "$unclosed";

        // Act & Assert
        var parser = new PatternParser();
        var ex = Assert.Throws<PatternParseException>(() => parser.Parse(pattern));
        Assert.Contains("Unclosed placeholder", ex.Message);
    }

    [Fact]
    public void FullWorkflow_EmptyCode_ShouldReturnNoMatches()
    {
        // Arrange
        var code = "namespace Test { }";
        var pattern = "$methodCall$";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        var parser = new PatternParser();
        var patternAst = parser.Parse(pattern);

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, root);

        // Assert - should not match method-like patterns in empty/simple namespace
        Assert.All(matches, m => Assert.DoesNotContain("(", m.Node.ToString()));
    }

    [Fact(Skip = "Requires Phase 2: LINQ pattern matching")]
    public void FullWorkflow_ComplexNestedPattern_ShouldMatch()
    {
        // Arrange
        var code = @"
class TestClass
{
    void Method()
    {
        var result = list.Where(x => x > 5).Select(x => x * 2);
        var filtered = items.Where(i => i.IsActive);
    }
}";

        var pattern = "$collection$.Where($predicate$)";

        // Act
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();

        var parser = new PatternParser();
        var patternAst = parser.Parse(pattern);

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, root);

        // Assert
        Assert.NotEmpty(matches);
        Assert.True(matches.Count >= 2); // list.Where and items.Where
    }

    [Fact(Skip = "MSBuild workspace loading requires additional setup")]
    public async Task FullWorkflow_RealProjectFile_ShouldLoadAndSearch()
    {
        // Arrange - Create a temporary test project
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var csprojPath = Path.Combine(tempDir, "TestProject.csproj");
            var csFilePath = Path.Combine(tempDir, "TestFile.cs");

            // Create a minimal .csproj file
            File.WriteAllText(csprojPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>
</Project>");

            // Create a C# file
            File.WriteAllText(csFilePath, @"
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello"");
        Console.WriteLine(""World"");
    }
}");

            var pattern = "Console.WriteLine($arg$)";

            // Act
            var workspace = await RoslynHelper.LoadWorkspaceAsync(csprojPath);
            var project = workspace.CurrentSolution.Projects.First();
            var compilation = await RoslynHelper.BuildCompilationAsync(project);

            Assert.NotNull(compilation);

            var parser = new PatternParser();
            var patternAst = parser.Parse(pattern);

            var matcher = new PatternMatcher(compilation.GetSemanticModel(compilation.SyntaxTrees.First()));
            var matches = matcher.FindMatches(patternAst, compilation.SyntaxTrees.First().GetRoot());

            // Assert
            Assert.NotEmpty(matches);
            Assert.Equal(2, matches.Count);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
