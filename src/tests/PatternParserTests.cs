using SharpCodeSearch.Services;

using Xunit;

namespace SharpCodeSearch.Tests;

public class PatternParserTests
{
    private readonly PatternParser _parser;

    public PatternParserTests()
    {
        _parser = new PatternParser();
    }

    [Fact]
    public void Parse_SimpleText_CreatesTextNode()
    {
        var result = _parser.Parse("hello world");

        Assert.Single(result.Nodes);
        var textNode = Assert.IsType<Models.TextNode>(result.Nodes[0]);
        Assert.Equal("hello world", textNode.Text);
    }

    [Fact]
    public void Parse_SimplePlaceholder_CreatesPlaceholderNode()
    {
        var result = _parser.Parse("$var$");

        Assert.Single(result.Nodes);
        var placeholder = Assert.IsType<Models.PlaceholderNode>(result.Nodes[0]);
        Assert.Equal("var", placeholder.Name);
    }

    [Fact]
    public void Parse_TextAndPlaceholder_CreatesBothNodes()
    {
        var result = _parser.Parse("int $var$ = 0;");

        Assert.Equal(3, result.Nodes.Count);
        Assert.IsType<Models.TextNode>(result.Nodes[0]);
        Assert.IsType<Models.PlaceholderNode>(result.Nodes[1]);
        Assert.IsType<Models.TextNode>(result.Nodes[2]);
    }

    [Fact]
    public void Parse_MultiplePlaceholders_CreatesMultipleNodes()
    {
        var result = _parser.Parse("$obj$.$method$($args$)");

        Assert.Equal(6, result.Nodes.Count);
        var obj = Assert.IsType<Models.PlaceholderNode>(result.Nodes[0]);
        var dot = Assert.IsType<Models.TextNode>(result.Nodes[1]);
        var method = Assert.IsType<Models.PlaceholderNode>(result.Nodes[2]);
        var args = Assert.IsType<Models.PlaceholderNode>(result.Nodes[4]);

        Assert.Equal("obj", obj.Name);
        Assert.Equal(".", dot.Text);
        Assert.Equal("method", method.Name);
        Assert.Equal("args", args.Name);
    }

    [Fact]
    public void Tokenize_SimpleText_ReturnsSingleToken()
    {
        var tokens = _parser.Tokenize("hello");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Text, tokens[0].Type);
        Assert.Equal("hello", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_Placeholder_ReturnsSingleToken()
    {
        var tokens = _parser.Tokenize("$var$");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Placeholder, tokens[0].Type);
        Assert.Equal("var", tokens[0].Value);
    }

    [Fact]
    public void Tokenize_UnclosedPlaceholder_ThrowsException()
    {
        var ex = Assert.Throws<PatternParseException>(() => _parser.Tokenize("$var"));
        Assert.Contains("Unclosed placeholder", ex.Message);
        Assert.Equal(0, ex.Position);
    }

    [Fact]
    public void Tokenize_EmptyPlaceholder_ReturnsEmptyValue()
    {
        var tokens = _parser.Tokenize("$$");

        Assert.Single(tokens);
        Assert.Equal(TokenType.Placeholder, tokens[0].Type);
        Assert.Empty(tokens[0].Value);
    }

    [Fact]
    public void Tokenize_PositionTracking_IsCorrect()
    {
        var tokens = _parser.Tokenize("int $var$ = 0");

        Assert.Equal(0, tokens[0].Position);
        Assert.Equal(4, tokens[1].Position);
        Assert.Equal(9, tokens[2].Position);
    }

    [Fact]
    public void ValidatePattern_ValidPattern_ReturnsTrue()
    {
        var result = _parser.ValidatePattern("$obj$.ToString()");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidatePattern_EmptyPattern_ReturnsFalse()
    {
        var result = _parser.ValidatePattern("");

        Assert.False(result.IsValid);
        Assert.Contains("Pattern cannot be empty", result.Errors);
    }

    [Fact]
    public void ValidatePattern_UnclosedPlaceholder_ReturnsFalse()
    {
        var result = _parser.ValidatePattern("$var");

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void ValidatePattern_WhitespacePattern_ReturnsFalse()
    {
        var result = _parser.ValidatePattern("   ");

        Assert.False(result.IsValid);
        Assert.Contains("Pattern cannot be empty", result.Errors);
    }

    [Fact]
    public void Parse_InfersExpressionType_FromName()
    {
        var result = _parser.Parse("$expr$");
        var placeholder = Assert.IsType<Models.PlaceholderNode>(result.Nodes[0]);
        Assert.Equal(Models.PlaceholderType.Expression, placeholder.Type);
    }

    [Fact]
    public void Parse_InfersStatementType_FromName()
    {
        var result = _parser.Parse("$stmt$");
        var placeholder = Assert.IsType<Models.PlaceholderNode>(result.Nodes[0]);
        Assert.Equal(Models.PlaceholderType.Statement, placeholder.Type);
    }

    [Fact]
    public void Parse_InfersArgumentsType_FromName()
    {
        var result = _parser.Parse("Method($args$)");
        var placeholder = Assert.IsType<Models.PlaceholderNode>(result.Nodes[1]);
        Assert.Equal(Models.PlaceholderType.Arguments, placeholder.Type);
    }

    [Fact]
    public void Parse_InfersIdentifierType_FromName()
    {
        var result = _parser.Parse("$var$");
        var placeholder = Assert.IsType<Models.PlaceholderNode>(result.Nodes[0]);
        Assert.Equal(Models.PlaceholderType.Identifier, placeholder.Type);
    }

    [Fact]
    public void Parse_InfersTypeType_FromName()
    {
        var result = _parser.Parse("$type$ field;");
        var placeholder = Assert.IsType<Models.PlaceholderNode>(result.Nodes[0]);
        Assert.Equal(Models.PlaceholderType.Type, placeholder.Type);
    }

    [Fact]
    public void Parse_InfersMemberType_FromName()
    {
        var result = _parser.Parse("obj.$member$");
        var placeholder = Assert.IsType<Models.PlaceholderNode>(result.Nodes[1]);
        Assert.Equal(Models.PlaceholderType.Member, placeholder.Type);
    }

    [Fact]
    public void Parse_DefaultsToAnyType_ForUnknownNames()
    {
        var result = _parser.Parse("$xyz$");
        var placeholder = Assert.IsType<Models.PlaceholderNode>(result.Nodes[0]);
        Assert.Equal(Models.PlaceholderType.Any, placeholder.Type);
    }

    [Fact]
    public void Parse_NullPattern_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _parser.Parse(null!));
    }

    [Fact]
    public void Parse_PreservesOriginalPattern()
    {
        var original = "$obj$.$method$()";
        var result = _parser.Parse(original);
        Assert.Equal(original, result.OriginalPattern);
    }

    [Fact]
    public void Parse_ComplexPattern_ParsesCorrectly()
    {
        var result = _parser.Parse("if ($condition$) { $stmt$; }");
        Assert.Equal(5, result.Nodes.Count);
    }
}
