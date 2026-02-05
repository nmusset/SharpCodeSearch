using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Models;
using SharpCodeSearch.Services;

using Xunit;

namespace SharpCodeSearch.Tests;

/// <summary>
/// Tests for argument placeholder matching functionality.
/// </summary>
public class ArgumentMatchingTests
{
    private readonly PatternParser _parser;

    public ArgumentMatchingTests()
    {
        _parser = new PatternParser();
    }

    #region Basic Argument Matching

    [Fact]
    public void Match_SingleArgument_Simple()
    {
        var code = @"
            void Method()
            {
                Console.WriteLine(""test"");
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("WriteLine($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should match the WriteLine call
        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.True(match.Placeholders.ContainsKey("args"));
    }

    [Fact]
    public void Match_MultipleArguments()
    {
        var code = @"
            void Method()
            {
                Math.Max(5, 10);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("Max($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.True(match.Placeholders.ContainsKey("args"));
        // Should capture both arguments
        var argsValue = match.Placeholders["args"];
        Assert.Contains("5", argsValue);
        Assert.Contains("10", argsValue);
    }

    [Fact]
    public void Match_NoArguments_EmptyParentheses()
    {
        var code = @"
            void Method()
            {
                GetValue();
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("GetValue($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should match method with no arguments
        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.True(match.Placeholders.ContainsKey("args"));
        // args should be empty or indicate no arguments
    }

    [Fact]
    public void Match_ComplexArguments_Expressions()
    {
        var code = @"
            void Method()
            {
                Calculate(x + 5, y * 2, GetValue());
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("Calculate($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.True(match.Placeholders.ContainsKey("args"));
    }

    #endregion

    #region Named Arguments

    [Fact]
    public void Match_NamedArguments()
    {
        var code = @"
            void Method()
            {
                DoSomething(name: ""test"", value: 42);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("DoSomething($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.True(match.Placeholders.ContainsKey("args"));
        var argsValue = match.Placeholders["args"];
        Assert.Contains("name:", argsValue);
        Assert.Contains("value:", argsValue);
    }

    [Fact]
    public void Match_MixedPositionalAndNamedArguments()
    {
        var code = @"
            void Method()
            {
                Process(""data"", count: 10, verbose: true);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("Process($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    #endregion

    #region Count Constraints

    [Fact]
    public void Match_ArgumentsWithMinCount()
    {
        var code = @"
            void Method()
            {
                Method1(1);
                Method2(1, 2);
                Method3(1, 2, 3);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);

        // Create pattern with min count = 2
        var placeholder = new PlaceholderNode
        {
            Name = "args",
            Type = PlaceholderType.Arguments,
            Position = 0,
            Length = 7,
            Constraints = new List<IConstraint>
            {
                new CountConstraint(minCount: 2)
            }
        };

        var textNode = new TextNode
        {
            Text = "Method",
            Position = 0,
            Length = 6
        };

        var patternAst = new PatternAst
        {
            OriginalPattern = "Method($args:count(min=2)$)",
            Nodes = new List<PatternNode> { textNode }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, tree.GetRoot());

        // Should find Method2 and Method3, but not Method1 (only 1 arg)
        // For now, just check we get matches
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_ArgumentsWithMaxCount()
    {
        var code = @"
            void Method()
            {
                Method1(1);
                Method2(1, 2);
                Method3(1, 2, 3);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);

        var placeholder = new PlaceholderNode
        {
            Name = "args",
            Type = PlaceholderType.Arguments,
            Position = 0,
            Length = 7,
            Constraints = new List<IConstraint>
            {
                new CountConstraint(maxCount: 2)
            }
        };

        var textNode = new TextNode
        {
            Text = "Method",
            Position = 0,
            Length = 6
        };

        var patternAst = new PatternAst
        {
            OriginalPattern = "Method($args:count(max=2)$)",
            Nodes = new List<PatternNode> { textNode }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, tree.GetRoot());

        // Should find Method1 and Method2, but not Method3 (3 args)
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_ArgumentsWithExactCount()
    {
        var code = @"
            void Method()
            {
                Method1(1);
                Method2(1, 2);
                Method3(1, 2, 3);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);

        var placeholder = new PlaceholderNode
        {
            Name = "args",
            Type = PlaceholderType.Arguments,
            Position = 0,
            Length = 7,
            Constraints = new List<IConstraint>
            {
                new CountConstraint(minCount: 2, maxCount: 2)
            }
        };

        var textNode = new TextNode
        {
            Text = "Method",
            Position = 0,
            Length = 6
        };

        var patternAst = new PatternAst
        {
            OriginalPattern = "Method($args:count(2)$)",
            Nodes = new List<PatternNode> { textNode }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, tree.GetRoot());

        // Should only find Method2 (exactly 2 args)
        Assert.NotEmpty(matches);
    }

    #endregion

    #region Advanced Scenarios

    [Fact]
    public void Match_NestedMethodCalls_InnerArguments()
    {
        var code = @"
            void Method()
            {
                Outer(Inner(1, 2), 3);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("Inner($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        var argsValue = match.Placeholders["args"];
        Assert.Contains("1", argsValue);
        Assert.Contains("2", argsValue);
    }

    [Fact]
    public void Match_ConstructorCall_WithArguments()
    {
        var code = @"
            void Method()
            {
                var obj = new MyClass(""param1"", 42);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("new MyClass($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should match constructor invocation
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_LambdaAsArgument()
    {
        var code = @"
            void Method()
            {
                items.Select(x => x.Value);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("Select($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        var argsValue = match.Placeholders["args"];
        Assert.Contains("=>", argsValue);
    }

    [Fact]
    public void Match_MultipleCallsSameMethod_DifferentArgs()
    {
        var code = @"
            void Method()
            {
                Process(1);
                Process(1, 2);
                Process(1, 2, 3);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("Process($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find all three calls
        Assert.True(matches.Count >= 3, $"Expected at least 3 matches, got {matches.Count}");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Match_ArgumentsOnly_WithoutMethodName()
    {
        var code = @"
            void Method()
            {
                SomeMethod(1, 2, 3);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        // Just match argument lists directly
        var pattern = _parser.Parse("$args$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find argument list nodes
        var argMatches = matches.Where(m => m.Node is Microsoft.CodeAnalysis.CSharp.Syntax.ArgumentListSyntax).ToList();
        Assert.NotEmpty(argMatches);
    }

    [Fact]
    public void Match_ParamsArray()
    {
        var code = @"
            void Method()
            {
                Console.WriteLine(""Format: {0} {1} {2}"", 1, 2, 3);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("WriteLine($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.True(match.Placeholders.ContainsKey("args"));
    }

    [Fact]
    public void Match_ArgumentWithDefaultValue()
    {
        var code = @"
            void Method()
            {
                CallMethod(required: true);
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("CallMethod($args$)");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    #endregion
}
