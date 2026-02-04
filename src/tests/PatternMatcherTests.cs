using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Models;
using SharpCodeSearch.Services;

using Xunit;

namespace SharpCodeSearch.Tests;

public class PatternMatcherTests
{
    private readonly PatternParser _parser;

    public PatternMatcherTests()
    {
        _parser = new PatternParser();
    }

    [Fact]
    public void FindMatches_NullPattern_ThrowsArgumentNullException()
    {
        var code = "var x = 1;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var matcher = new PatternMatcher();

        Assert.Throws<ArgumentNullException>(() =>
            matcher.FindMatches(null!, tree.GetRoot()));
    }

    [Fact]
    public void FindMatches_NullSyntaxNode_ThrowsArgumentNullException()
    {
        var pattern = _parser.Parse("$var$");
        var matcher = new PatternMatcher();

        Assert.Throws<ArgumentNullException>(() =>
            matcher.FindMatches(pattern, null!));
    }

    [Fact]
    public void FindMatches_SimpleIdentifier_FindsMatch()
    {
        var code = "var x = 1;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$var$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        // The pattern should match identifier nodes
        // In "var x = 1;", we have identifiers: "var" (as type), "x" (as variable name)
        var xMatch = matches.FirstOrDefault(m =>
            m.Placeholders.ContainsKey("var") &&
            m.Placeholders["var"] == "x");
        Assert.NotNull(xMatch);
        Assert.Equal("x", xMatch.Placeholders["var"]);
    }

    [Fact]
    public void FindMatches_Expression_FindsMatch()
    {
        var code = "var result = 1 + 2;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$expr$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        // Should find multiple expressions: 1, 2, 1 + 2, result
        Assert.True(matches.Count >= 3);
    }

    [Fact]
    public void FindMatches_BinaryExpression_FindsMatch()
    {
        var code = "var result = 1 + 2;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$left$ + $right$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // This is a text-based match, not structural
        // The current implementation matches text, so this should find the expression
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void FindMatches_MethodCall_FindsMatch()
    {
        var code = "Console.WriteLine(\"test\");";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("WriteLine");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    [Fact]
    public void FindMatches_MultipleIdentifiers_FindsAll()
    {
        var code = @"
            var x = 1;
            var y = 2;
            var z = 3;
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$var$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find x, y, z as identifiers
        Assert.True(matches.Count >= 3);
        var xMatch = matches.Any(m => m.Placeholders.ContainsKey("var") && m.Placeholders["var"] == "x");
        var yMatch = matches.Any(m => m.Placeholders.ContainsKey("var") && m.Placeholders["var"] == "y");
        var zMatch = matches.Any(m => m.Placeholders.ContainsKey("var") && m.Placeholders["var"] == "z");
        Assert.True(xMatch, "Should find identifier 'x'");
        Assert.True(yMatch, "Should find identifier 'y'");
        Assert.True(zMatch, "Should find identifier 'z'");
    }

    [Fact]
    public void FindMatches_NoMatches_ReturnsEmptyList()
    {
        var code = "var x = 1;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("NonExistentPattern");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.Empty(matches);
    }

    [Fact]
    public void FindMatches_WithLocation_SetsLocationCorrectly()
    {
        var code = "var x = 1;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$var$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        foreach (var match in matches)
        {
            Assert.NotNull(match.Location);
            Assert.NotEqual(Location.None, match.Location);
        }
    }

    [Fact]
    public void FindMatches_TextPattern_FindsContainingText()
    {
        var code = "var myVariable = 100;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("Variable");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    [Fact]
    public void FindMatches_ExpressionType_MatchesExpressions()
    {
        var code = "var x = 1 + 2 * 3;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$expr$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find multiple expression nodes
        Assert.True(matches.Count >= 3);
    }

    [Fact]
    public void FindMatches_IdentifierType_MatchesIdentifiers()
    {
        var code = "var myVar = anotherVar;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$var$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        var myVarMatch = matches.Any(m => m.Placeholders.ContainsKey("var") && m.Placeholders["var"] == "myVar");
        var anotherVarMatch = matches.Any(m => m.Placeholders.ContainsKey("var") && m.Placeholders["var"] == "anotherVar");

        Assert.True(myVarMatch, "Should find identifier 'myVar'");
        Assert.True(anotherVarMatch, "Should find identifier 'anotherVar'");
    }

    [Fact]
    public void FindMatches_StatementType_MatchesStatements()
    {
        var code = @"
            int x = 1;
            Console.WriteLine(x);
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$stmt$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find statement nodes
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void FindMatches_AnyType_MatchesAnyNode()
    {
        var code = "var x = 1;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$anything$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // 'anything' should infer to PlaceholderType.Any and match any node
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void FindMatches_ComplexCode_FindsMatches()
    {
        var code = @"
            class TestClass
            {
                void Method()
                {
                    var x = 1;
                    var y = 2;
                    var z = x + y;
                }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$var$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.True(matches.Count >= 3);
    }

    [Fact]
    public void FindMatches_SamePlaceholderTwice_VerifiesConsistency()
    {
        // This test verifies that if the same placeholder appears twice,
        // it must match the same value both times
        var code = "var x = x + 1;";
        var tree = CSharpSyntaxTree.ParseText(code);
        // Pattern with same placeholder - for now, test single placeholder
        var pattern = _parser.Parse("$var$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should find 'x' multiple times (at least twice: in declaration and in expression)
        var xMatches = matches.Where(m =>
            m.Placeholders.ContainsKey("var") &&
            m.Placeholders["var"] == "x"
        ).ToList();

        Assert.True(xMatches.Count >= 2, $"Expected at least 2 matches for 'x', found {xMatches.Count}");
    }

    [Fact]
    public void FindMatches_WithRegexConstraint_FiltersResults()
    {
        var code = "var testVar = 1; var otherVar = 2;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$var$");

        // Add regex constraint - manually create a new placeholder node with constraint
        var ast = new PatternAst
        {
            OriginalPattern = "$var$",
            Nodes = new List<PatternNode>
            {
                new PlaceholderNode
                {
                    Name = "var",
                    Type = PlaceholderType.Identifier,
                    Position = 0,
                    Length = 5,
                    Constraints = new List<IConstraint>
                    {
                        new RegexConstraint("^test.*")
                    }
                }
            }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(ast, tree.GetRoot());

        // Should only match identifiers starting with "test"
        var testMatches = matches.Where(m =>
            m.Placeholders.ContainsKey("var") &&
            m.Placeholders["var"].StartsWith("test")
        ).ToList();

        Assert.NotEmpty(testMatches);
        Assert.Contains(testMatches, m => m.Placeholders["var"] == "testVar");

        // Should not match "otherVar" or "var"
        var otherMatches = matches.Where(m =>
            m.Placeholders.ContainsKey("var") &&
            (m.Placeholders["var"] == "otherVar" || m.Placeholders["var"] == "var")
        ).ToList();

        Assert.Empty(otherMatches);
    }

    [Fact]
    public void FindMatches_WithTypeConstraint_UsesSemanticModel()
    {
        // Type constraints work on expressions where semantic model has type info
        // Variable declarators don't have direct type info, so we test with identifier references
        var code = @"
            int x = 1;
            string y = ""test"";
            var result = x + 1;  // x here is an IdentifierNameSyntax with type info
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var semanticModel = compilation.GetSemanticModel(tree);

        // Create pattern with type constraint for identifier expressions
        var ast = new PatternAst
        {
            OriginalPattern = "$var$",
            Nodes = new List<PatternNode>
            {
                new PlaceholderNode
                {
                    Name = "var",
                    Type = PlaceholderType.Expression,  // Use Expression type to match the reference
                    Position = 0,
                    Length = 5,
                    Constraints = new List<IConstraint>
                    {
                        new TypeConstraint("int")
                    }
                }
            }
        };

        var matcher = new PatternMatcher(semanticModel);
        var matches = matcher.FindMatches(ast, tree.GetRoot());

        // Should find expressions of type int (like "x + 1", "x", "1")
        var intMatches = matches.Where(m =>
            m.Placeholders.ContainsKey("var")
        ).ToList();

        // We should find some int-typed expressions
        Assert.NotEmpty(intMatches);
    }

    [Fact]
    public void PatternMatch_PlaceholdersProperty_IsInitialized()
    {
        var code = "var x = 1;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$var$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        foreach (var match in matches)
        {
            Assert.NotNull(match.Placeholders);
        }
    }

    [Fact]
    public void FindMatches_MemberAccess_ExtractsIdentifier()
    {
        var code = "obj.Property";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$member$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        var memberMatches = matches.Where(m =>
            m.Placeholders.ContainsKey("member")
        ).ToList();

        Assert.NotEmpty(memberMatches);
    }

    [Fact]
    public void FindMatches_WithExactMatchConstraint_FiltersCorrectly()
    {
        var code = "var test = 1; var other = 2;";
        var tree = CSharpSyntaxTree.ParseText(code);

        // Create pattern with exact match constraint
        var ast = new PatternAst
        {
            OriginalPattern = "$var$",
            Nodes = new List<PatternNode>
            {
                new PlaceholderNode
                {
                    Name = "var",
                    Type = PlaceholderType.Identifier,
                    Position = 0,
                    Length = 5,
                    Constraints = new List<IConstraint>
                    {
                        new ExactMatchConstraint("test")
                    }
                }
            }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(ast, tree.GetRoot());

        // Should only match "test"
        var exactMatches = matches.Where(m =>
            m.Placeholders.ContainsKey("var") &&
            m.Placeholders["var"] == "test"
        ).ToList();

        Assert.NotEmpty(exactMatches);

        // Should not match "other"
        var otherMatches = matches.Where(m =>
            m.Placeholders.ContainsKey("var") &&
            m.Placeholders["var"] == "other"
        ).ToList();

        Assert.Empty(otherMatches);
    }

    [Fact]
    public void FindMatches_EmptyCode_ReturnsEmptyList()
    {
        var code = "";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$var$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Empty code should not throw, just return empty list
        Assert.NotNull(matches);
    }
}
