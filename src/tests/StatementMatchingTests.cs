using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Models;
using SharpCodeSearch.Services;

using Xunit;

namespace SharpCodeSearch.Tests;

/// <summary>
/// Tests for statement placeholder matching functionality.
/// </summary>
public class StatementMatchingTests
{
    private readonly PatternParser _parser;

    public StatementMatchingTests()
    {
        _parser = new PatternParser();
    }

    #region Single Statement Tests

    [Fact]
    public void Match_SingleStatement_Explicit()
    {
        var code = @"
            void Method()
            {
                int x = 1;
                Console.WriteLine(x);
                return;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should match each statement individually
        Assert.True(matches.Count >= 3, $"Expected at least 3 statements, got {matches.Count}");
    }

    [Fact]
    public void Match_SingleStatement_ReturnStatement()
    {
        var code = @"
            void Method()
            {
                return;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("return;");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.Single(matches);
    }

    [Fact]
    public void Match_SingleStatement_WithContext()
    {
        var code = @"
            void Method()
            {
                int x = 1;
                if (x > 0)
                {
                    Console.WriteLine(x);
                }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        // Pattern: any statement followed by if
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find multiple statements
        Assert.NotEmpty(matches);
    }

    #endregion

    #region Block Statement Tests

    [Fact]
    public void Match_BlockStatement_MultipleStatements()
    {
        var code = @"
            void Method()
            {
                int x = 1;
                int y = 2;
                int z = 3;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        // This should eventually support block matching
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // For now, should match individual statements
        Assert.True(matches.Count >= 3, $"Expected at least 3 statements, got {matches.Count}");
    }

    [Fact]
    public void Match_BlockStatement_InsideIf()
    {
        var code = @"
            void Method()
            {
                if (true)
                {
                    int x = 1;
                    Console.WriteLine(x);
                }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find the if statement and statements inside
        Assert.NotEmpty(matches);
    }

    #endregion

    #region Count Constraint Tests

    [Fact]
    public void Match_StatementWithMinCount()
    {
        var code = @"
            void Method()
            {
                int x = 1;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);

        // Create pattern with count constraint programmatically
        var placeholder = new PlaceholderNode
        {
            Name = "stmt",
            Type = PlaceholderType.Statement,
            Position = 0,
            Length = 7,
            Constraints = new List<IConstraint>
            {
                new CountConstraint(minCount: 1)
            }
        };

        var patternAst = new PatternAst
        {
            OriginalPattern = "$stmt:count(min=1)$",
            Nodes = new List<PatternNode> { placeholder }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, tree.GetRoot());

        // Should match statements
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_StatementWithMaxCount()
    {
        var code = @"
            void Method()
            {
                int x = 1;
                int y = 2;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);

        var placeholder = new PlaceholderNode
        {
            Name = "stmt",
            Type = PlaceholderType.Statement,
            Position = 0,
            Length = 7,
            Constraints = new List<IConstraint>
            {
                new CountConstraint(maxCount: 5)
            }
        };

        var patternAst = new PatternAst
        {
            OriginalPattern = "$stmt:count(max=5)$",
            Nodes = new List<PatternNode> { placeholder }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, tree.GetRoot());

        // Should match statements within limit
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_StatementWithRangeCount()
    {
        var code = @"
            void Method()
            {
                int x = 1;
                int y = 2;
                int z = 3;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);

        var placeholder = new PlaceholderNode
        {
            Name = "stmts",
            Type = PlaceholderType.Statement,
            Position = 0,
            Length = 8,
            Constraints = new List<IConstraint>
            {
                new CountConstraint(minCount: 1, maxCount: 5)
            }
        };

        var patternAst = new PatternAst
        {
            OriginalPattern = "$stmts:count(1-5)$",
            Nodes = new List<PatternNode> { placeholder }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, tree.GetRoot());

        // Should match statements within range
        Assert.NotEmpty(matches);
    }

    #endregion

    #region Complex Pattern Tests

    [Fact]
    public void Match_StatementBeforeReturn()
    {
        var code = @"
            int Method()
            {
                int result = 42;
                return result;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);

        // This pattern should eventually match: any statement followed by return
        // For now, test individual statement matching
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.True(matches.Count >= 2, "Should find at least the assignment and return");
    }

    [Fact]
    public void Match_MultipleStatementsPattern()
    {
        var code = @"
            void Method()
            {
                var x = GetValue();
                Console.WriteLine(x);
                Dispose();
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find each statement
        Assert.True(matches.Count >= 3, $"Expected at least 3 matches, got {matches.Count}");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Match_EmptyBlock_NoStatements()
    {
        var code = @"
            void Method()
            {
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should not match anything in an empty block
        Assert.Empty(matches);
    }

    [Fact]
    public void Match_NestedStatements()
    {
        var code = @"
            void Method()
            {
                if (true)
                {
                    while (true)
                    {
                        Console.WriteLine(""nested"");
                    }
                }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find nested statements
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_ExpressionStatement()
    {
        var code = @"
            void Method()
            {
                Console.WriteLine(""test"");
                x++;
                DoSomething();
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // All three should be matched as statements
        Assert.True(matches.Count >= 3, $"Expected at least 3 matches, got {matches.Count}");
    }

    #endregion
}
