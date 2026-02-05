using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SharpCodeSearch.Models;
using SharpCodeSearch.Services;

using Xunit;

namespace SharpCodeSearch.Tests;

/// <summary>
/// Tests for type placeholder matching functionality.
/// </summary>
public class TypeMatchingTests
{
    private readonly PatternParser _parser;

    public TypeMatchingTests()
    {
        _parser = new PatternParser();
    }

    #region Basic Type Matching

    [Fact]
    public void Match_SimpleType_VariableDeclaration()
    {
        var code = @"
            class Test
            {
                void Method()
                {
                    int x = 5;
                    string name = ""test"";
                }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$ x");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.FirstOrDefault(m => m.Placeholders.ContainsKey("type"));
        Assert.NotNull(match);
        Assert.Equal("int", match.Placeholders["type"]);
    }

    [Fact]
    public void Match_SimpleType_FieldDeclaration()
    {
        var code = @"
            class Test
            {
                private int count;
                private string message;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$ count");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_SimpleType_ParameterDeclaration()
    {
        var code = @"
            class Test
            {
                void Method(int value, string name)
                {
                }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$ value");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.Equal("int", match.Placeholders["type"]);
    }

    #endregion

    #region Generic Type Matching

    [Fact(Skip = "Deferred: Generic type argument matching")]
    public void Match_GenericType_List()
    {
        var code = @"
            using System.Collections.Generic;
            class Test
            {
                void Method()
                {
                    List<int> numbers = new List<int>();
                    List<string> names = new List<string>();
                }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("List<$type$> numbers");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.Equal("int", match.Placeholders["type"]);
    }

    [Fact(Skip = "Deferred: Generic type argument matching")]
    public void Match_GenericType_Dictionary()
    {
        var code = @"
            using System.Collections.Generic;
            class Test
            {
                Dictionary<string, int> map;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("Dictionary<$keyType$, $valueType$> map");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.Equal("string", match.Placeholders["keyType"]);
        Assert.Equal("int", match.Placeholders["valueType"]);
    }

    [Fact(Skip = "Deferred: Generic type argument matching")]
    public void Match_GenericType_Nested()
    {
        var code = @"
            using System.Collections.Generic;
            class Test
            {
                List<List<int>> matrix;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("List<List<$type$>> matrix");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.Equal("int", match.Placeholders["type"]);
    }

    #endregion

    #region Array Type Matching

    [Fact(Skip = "Deferred: Array type matching")]
    public void Match_ArrayType_SingleDimension()
    {
        var code = @"
            class Test
            {
                int[] numbers;
                string[] names;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$[] numbers");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.Equal("int", match.Placeholders["type"]);
    }

    [Fact(Skip = "Deferred: Array type matching")]
    public void Match_ArrayType_MultiDimension()
    {
        var code = @"
            class Test
            {
                int[,] matrix;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$[,] matrix");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    #endregion

    #region Nullable Type Matching

    [Fact(Skip = "Deferred: Nullable type matching")]
    public void Match_NullableType_ValueType()
    {
        var code = @"
            class Test
            {
                int? count;
                DateTime? timestamp;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$? count");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.Equal("int", match.Placeholders["type"]);
    }

    [Fact(Skip = "Deferred: Nullable type matching")]
    public void Match_NullableReferenceType()
    {
        var code = @"
            #nullable enable
            class Test
            {
                string? nullableString;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$? nullableString");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    #endregion

    #region Type Placeholder Only

    [Fact(Skip = "Deferred: Standalone type placeholder matching")]
    public void Match_TypePlaceholder_Standalone()
    {
        var code = @"
            class Test
            {
                int count;
                string name;
                List<int> items;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should match all TypeSyntax nodes
        var typeMatches = matches.Where(m => m.Node is TypeSyntax).ToList();
        Assert.NotEmpty(typeMatches);
        Assert.True(typeMatches.Count >= 3, $"Expected at least 3 type matches, got {typeMatches.Count}");
    }

    #endregion

    #region Type Constraint Matching

    [Fact]
    public void Match_TypeConstraint_SpecificType()
    {
        var code = @"
            class Test
            {
                int count;
                string name;
                double value;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);

        // Create pattern with type constraint
        var placeholder = new PlaceholderNode
        {
            Name = "type",
            Type = PlaceholderType.Type,
            Position = 0,
            Length = 7,
            Constraints = new List<IConstraint>
            {
                new TypeConstraint("int")
            }
        };

        var textNode = new TextNode
        {
            Text = " count",
            Position = 7,
            Length = 6
        };

        var patternAst = new PatternAst
        {
            OriginalPattern = "$type:type(int)$ count",
            Nodes = new List<PatternNode> { placeholder, textNode }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, tree.GetRoot());

        // Should only match int type
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_TypeConstraint_Regex()
    {
        var code = @"
            class Test
            {
                int intValue;
                string stringValue;
                double doubleValue;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);

        // Create pattern with regex constraint for types starting with 'int'
        var placeholder = new PlaceholderNode
        {
            Name = "type",
            Type = PlaceholderType.Type,
            Position = 0,
            Length = 7,
            Constraints = new List<IConstraint>
            {
                new RegexConstraint("^int.*")
            }
        };

        var patternAst = new PatternAst
        {
            OriginalPattern = "$type:regex=^int.*$",
            Nodes = new List<PatternNode> { placeholder }
        };

        var matcher = new PatternMatcher();
        var matches = matcher.FindMatches(patternAst, tree.GetRoot());

        // Should only match int types
        var typeMatches = matches.Where(m => m.Placeholders.ContainsKey("type")).ToList();
        Assert.NotEmpty(typeMatches);
        Assert.All(typeMatches, m => Assert.StartsWith("int", m.Placeholders["type"]));
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void Match_Type_InMethodReturnType()
    {
        var code = @"
            class Test
            {
                int GetCount() { return 0; }
                string GetName() { return """"; }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$ GetCount()");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    [Fact(Skip = "Deferred: Cast expression type matching")]
    public void Match_Type_InCastExpression()
    {
        var code = @"
            class Test
            {
                void Method()
                {
                    var x = (int)value;
                    var y = (string)obj;
                }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("($type$)value");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_Type_InPropertyDeclaration()
    {
        var code = @"
            class Test
            {
                public int Count { get; set; }
                public string Name { get; set; }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("public $type$ Count");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_MultipleTypes_InSamePattern()
    {
        var code = @"
            class Test
            {
                Dictionary<string, int> map;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("Dictionary<$t1$, $t2$>");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        Assert.NotEmpty(matches);
        var match = matches.First();
        Assert.True(match.Placeholders.ContainsKey("t1"));
        Assert.True(match.Placeholders.ContainsKey("t2"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Match_Type_Var_Keyword()
    {
        var code = @"
            class Test
            {
                void Method()
                {
                    var x = 5;
                }
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$ x");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should match 'var' as a type
        Assert.NotEmpty(matches);
    }

    [Fact]
    public void Match_Type_BuiltInAlias()
    {
        var code = @"
            class Test
            {
                System.Int32 count;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("$type$ count");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should match System.Int32
        Assert.NotEmpty(matches);
    }

    [Fact(Skip = "Deferred: Tuple type matching")]
    public void Match_Type_TupleType()
    {
        var code = @"
            class Test
            {
                (int, string) tuple;
            }
        ";
        var tree = CSharpSyntaxTree.ParseText(code);
        var pattern = _parser.Parse("($type1$, $type2$) tuple");
        var matcher = new PatternMatcher();

        var matches = matcher.FindMatches(pattern, tree.GetRoot());

        // Should match tuple types
        Assert.NotEmpty(matches);
    }

    #endregion
}
