using SharpCodeSearch.Models;

using Xunit;

namespace SharpCodeSearch.Tests;

public class PatternNodeTests
{
    [Fact]
    public void TextNode_ToString_ReturnsText()
    {
        var node = new TextNode { Text = "hello world", Position = 0, Length = 11 };
        Assert.Equal("hello world", node.ToString());
    }

    [Fact]
    public void TextNode_Position_IsSet()
    {
        var node = new TextNode { Text = "test", Position = 5, Length = 4 };
        Assert.Equal(5, node.Position);
        Assert.Equal(4, node.Length);
    }

    [Fact]
    public void PlaceholderNode_ToString_ReturnsFormattedName()
    {
        var node = new PlaceholderNode
        {
            Name = "var",
            Type = PlaceholderType.Identifier,
            Position = 0,
            Length = 5
        };
        Assert.Equal("$var$", node.ToString());
    }

    [Fact]
    public void PlaceholderNode_Constraints_InitializesAsEmptyList()
    {
        var node = new PlaceholderNode
        {
            Name = "test",
            Type = PlaceholderType.Any,
            Position = 0,
            Length = 0
        };
        Assert.NotNull(node.Constraints);
        Assert.Empty(node.Constraints);
    }

    [Fact]
    public void PlaceholderNode_CanAddConstraints()
    {
        var node = new PlaceholderNode
        {
            Name = "var",
            Type = PlaceholderType.Identifier,
            Position = 0,
            Length = 0,
            Constraints = new List<IConstraint>
            {
                new RegexConstraint("^test.*"),
                new TypeConstraint("int")
            }
        };

        Assert.Equal(2, node.Constraints.Count);
    }

    [Fact]
    public void PatternAst_ToString_ReturnsOriginalPattern()
    {
        var ast = new PatternAst
        {
            OriginalPattern = "$var$ = 0;",
            Nodes = new List<PatternNode>()
        };
        Assert.Equal("$var$ = 0;", ast.ToString());
    }

    [Fact]
    public void PatternAst_Nodes_InitializesAsEmptyList()
    {
        var ast = new PatternAst
        {
            OriginalPattern = "test",
            Nodes = new List<PatternNode>()
        };
        Assert.NotNull(ast.Nodes);
        Assert.Empty(ast.Nodes);
    }

    [Fact]
    public void PatternAst_CanAddNodes()
    {
        var ast = new PatternAst
        {
            OriginalPattern = "test $var$",
            Nodes = new List<PatternNode>
            {
                new TextNode { Text = "test ", Position = 0, Length = 5 },
                new PlaceholderNode { Name = "var", Type = PlaceholderType.Identifier, Position = 5, Length = 5 }
            }
        };

        Assert.Equal(2, ast.Nodes.Count);
    }

    [Fact]
    public void TextNode_Accept_CallsVisitorVisitText()
    {
        var node = new TextNode { Text = "test", Position = 0, Length = 4 };
        var visitor = new TestPatternVisitor();

        node.Accept(visitor);

        Assert.True(visitor.VisitTextCalled);
        Assert.False(visitor.VisitPlaceholderCalled);
    }

    [Fact]
    public void PlaceholderNode_Accept_CallsVisitorVisitPlaceholder()
    {
        var node = new PlaceholderNode
        {
            Name = "var",
            Type = PlaceholderType.Identifier,
            Position = 0,
            Length = 0
        };
        var visitor = new TestPatternVisitor();

        node.Accept(visitor);

        Assert.False(visitor.VisitTextCalled);
        Assert.True(visitor.VisitPlaceholderCalled);
    }

    private class TestPatternVisitor : IPatternVisitor
    {
        public bool VisitTextCalled { get; private set; }
        public bool VisitPlaceholderCalled { get; private set; }

        public void VisitText(TextNode node)
        {
            VisitTextCalled = true;
        }

        public void VisitPlaceholder(PlaceholderNode node)
        {
            VisitPlaceholderCalled = true;
        }
    }
}
