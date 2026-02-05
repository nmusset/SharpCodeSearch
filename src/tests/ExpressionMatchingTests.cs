using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Models;
using SharpCodeSearch.Services;

using Xunit;

namespace SharpCodeSearch.Tests;

/// <summary>
/// Tests for advanced expression placeholder matching ($expr$).
/// Tests basic expression matching, operator expressions, and complex nested patterns.
/// </summary>
public class ExpressionMatchingTests
{
    private readonly PatternParser _parser = new();
    private readonly PatternMatcher _matcher = new();

    #region Basic Expression Matching

    [Fact]
    public void Match_SimpleExpression_ShouldFindMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var x = 10 + 5;
                    var y = 20 * 3;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$expr$");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert - Should find many expressions
        Assert.NotEmpty(matches);
    }

    [Fact(Skip = "Deferred: Text-based expression matching without placeholders")]
    public void Match_BinaryExpression_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = a + b;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("a + b");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_ComplexExpression_ShouldFindMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = (a + b) * (c - d);
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$expr$");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.NotEmpty(matches);
    }

    #endregion

    #region Operator Expressions

    [Fact(Skip = "Deferred: Text-based expression matching")]
    public void Match_ArithmeticExpression_Addition_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var a = x + y;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("x + y");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
    }

    [Fact(Skip = "Deferred: Text-based expression matching")]
    public void Match_LogicalExpression_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = a && b || c;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("a && b");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
    }

    [Fact(Skip = "Deferred: Complex placeholder extraction in expressions")]
    public void Match_ComparisonExpression_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    if (x > 10) { }
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$expr$ > 10");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
        Assert.Equal("x", matches[0].Placeholders["expr"]);
    }

    #endregion

    #region Expression Patterns with Placeholders

    [Fact(Skip = "Deferred: Complex placeholder extraction in expressions")]
    public void Match_ExpressionPattern_WithVariable_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = variable + 10;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$expr$ + 10");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
        Assert.Equal("variable", matches[0].Placeholders["expr"]);
    }

    [Fact(Skip = "Deferred: Complex placeholder extraction in expressions")]
    public void Match_ExpressionPattern_BothSides_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = x + y;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$left$ + $right$");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.NotEmpty(matches);
        Assert.Contains(matches, m =>
            m.Placeholders.ContainsKey("left") && m.Placeholders["left"] == "x" &&
            m.Placeholders.ContainsKey("right") && m.Placeholders["right"] == "y");
    }

    [Fact(Skip = "Deferred: Complex placeholder extraction in expressions")]
    public void Match_MemberAccessExpression_WithPlaceholder_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = obj.ToString();
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$expr$.ToString()");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
        Assert.Equal("obj", matches[0].Placeholders["expr"]);
    }

    #endregion

    #region Operator Precedence

    [Fact(Skip = "Deferred: Text-based expression matching")]
    public void Match_OperatorPrecedence_Multiplication_ShouldMatchCorrectly()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = a + b * c;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("b * c");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert - Should find b * c as sub-expression
        Assert.Single(matches);
    }

    [Fact(Skip = "Deferred: Text-based expression matching")]
    public void Match_OperatorPrecedence_Parentheses_ShouldRespect()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = (a + b) * c;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("a + b");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert - Should find a + b inside parentheses
        Assert.Single(matches);
    }

    #endregion

    #region Unary Expressions

    [Fact(Skip = "Deferred: Text-based expression matching")]
    public void Match_UnaryExpression_Negation_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var x = -value;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("-value");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
    }

    [Fact(Skip = "Deferred: Complex placeholder extraction in expressions")]
    public void Match_UnaryExpression_WithPlaceholder_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var y = !flag;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("!$expr$");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
        Assert.Equal("flag", matches[0].Placeholders["expr"]);
    }

    #endregion

    # region Assignment Expressions

    [Fact(Skip = "Deferred: Text-based expression matching")]
    public void Match_AssignmentExpression_Simple_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    x = 10;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("x = 10");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
    }

    [Fact(Skip = "Deferred: Complex placeholder extraction in expressions")]
    public void Match_AssignmentExpression_WithPlaceholder_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    x = 10;
                    y = 20;
                    z = 30;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$var$ = $value$");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Equal(3, matches.Count);
        Assert.Contains(matches, m => m.Placeholders["var"] == "x" && m.Placeholders["value"] == "10");
        Assert.Contains(matches, m => m.Placeholders["var"] == "y" && m.Placeholders["value"] == "20");
        Assert.Contains(matches, m => m.Placeholders["var"] == "z" && m.Placeholders["value"] == "30");
    }

    [Fact(Skip = "Deferred: Text-based expression matching")]
    public void Match_CompoundAssignment_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    x += 5;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("x += 5");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
    }

    #endregion

    #region Conditional Expressions

    [Fact(Skip = "Deferred: Text-based expression matching")]
    public void Match_TernaryExpression_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = condition ? trueValue : falseValue;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("condition ? trueValue : falseValue");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
    }

    [Fact(Skip = "Deferred: Complex placeholder extraction in expressions")]
    public void Match_TernaryExpression_WithPlaceholder_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    var result = x > 10 ? y : z;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$cond$ ? $true$ : $false$");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
        Assert.Equal("x > 10", matches[0].Placeholders["cond"]);
        Assert.Equal("y", matches[0].Placeholders["true"]);
        Assert.Equal("z", matches[0].Placeholders["false"]);
    }

    #endregion

    #region Lambda Expressions

    [Fact(Skip = "Deferred: Text-based expression matching")]
    public void Match_LambdaExpression_Simple_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    Func<int, int> f = x => x * 2;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("x => x * 2");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
    }

    [Fact(Skip = "Deferred: Complex placeholder extraction in expressions")]
    public void Match_LambdaExpression_WithPlaceholder_ShouldMatch()
    {
        // Arrange
        var code = @"
            class Test {
                void Method() {
                    Func<int, int> f = x => x * 2;
                }
            }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$param$ => $expr$");

        // Act
        var matches = _matcher.FindMatches(pattern, tree.GetRoot());

        // Assert
        Assert.Single(matches);
        Assert.Equal("x", matches[0].Placeholders["param"]);
        Assert.Equal("x * 2", matches[0].Placeholders["expr"]);
    }

    #endregion
}
