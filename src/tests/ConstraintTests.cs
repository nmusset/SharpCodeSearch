using SharpCodeSearch.Models;

using Xunit;

namespace SharpCodeSearch.Tests;

public class ConstraintTests
{
    [Fact]
    public void RegexConstraint_Constructor_ValidPattern_Succeeds()
    {
        var constraint = new RegexConstraint("^test.*");
        Assert.Equal("^test.*", constraint.Pattern);
    }

    [Fact]
    public void RegexConstraint_Constructor_NullPattern_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RegexConstraint(null!));
    }

    [Fact]
    public void RegexConstraint_Constructor_EmptyPattern_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new RegexConstraint(""));
    }

    [Fact]
    public void RegexConstraint_Validate_MatchingValue_ReturnsTrue()
    {
        var constraint = new RegexConstraint("^test.*");
        Assert.True(constraint.Validate("testValue"));
    }

    [Fact]
    public void RegexConstraint_Validate_NonMatchingValue_ReturnsFalse()
    {
        var constraint = new RegexConstraint("^test.*");
        Assert.False(constraint.Validate("value"));
    }

    [Fact]
    public void RegexConstraint_Validate_NullValue_HandlesSafely()
    {
        var constraint = new RegexConstraint("^test.*");
        Assert.False(constraint.Validate(null!));
    }

    [Fact]
    public void RegexConstraint_Description_ReturnsCorrectFormat()
    {
        var constraint = new RegexConstraint("^test.*");
        Assert.Equal("regex(^test.*)", constraint.Description);
    }

    [Fact]
    public void TypeConstraint_Constructor_ValidTypeName_Succeeds()
    {
        var constraint = new TypeConstraint("int");
        Assert.Equal("int", constraint.TypeName);
    }

    [Fact]
    public void TypeConstraint_Constructor_NullTypeName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TypeConstraint(null!));
    }

    [Fact]
    public void TypeConstraint_Constructor_EmptyTypeName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TypeConstraint(""));
    }

    [Fact]
    public void TypeConstraint_Validate_NonEmptyValue_ReturnsTrue()
    {
        var constraint = new TypeConstraint("int");
        Assert.True(constraint.Validate("someValue"));
    }

    [Fact]
    public void TypeConstraint_Validate_EmptyValue_ReturnsFalse()
    {
        var constraint = new TypeConstraint("int");
        Assert.False(constraint.Validate(""));
    }

    [Fact]
    public void TypeConstraint_Description_ReturnsCorrectFormat()
    {
        var constraint = new TypeConstraint("string");
        Assert.Equal("type(string)", constraint.Description);
    }

    [Fact]
    public void CountConstraint_Constructor_BothMinMax_Succeeds()
    {
        var constraint = new CountConstraint(1, 5);
        Assert.Equal(1, constraint.MinCount);
        Assert.Equal(5, constraint.MaxCount);
    }

    [Fact]
    public void CountConstraint_Constructor_OnlyMin_Succeeds()
    {
        var constraint = new CountConstraint(minCount: 2);
        Assert.Equal(2, constraint.MinCount);
        Assert.Null(constraint.MaxCount);
    }

    [Fact]
    public void CountConstraint_Constructor_OnlyMax_Succeeds()
    {
        var constraint = new CountConstraint(maxCount: 10);
        Assert.Null(constraint.MinCount);
        Assert.Equal(10, constraint.MaxCount);
    }

    [Fact]
    public void CountConstraint_Constructor_MinGreaterThanMax_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new CountConstraint(10, 5));
    }

    [Fact]
    public void CountConstraint_Validate_ReturnsTrue()
    {
        var constraint = new CountConstraint(1, 5);
        Assert.True(constraint.Validate("anything"));
    }

    [Fact]
    public void CountConstraint_Description_BothMinMax_ReturnsCorrectFormat()
    {
        var constraint = new CountConstraint(1, 5);
        Assert.Equal("count(1-5)", constraint.Description);
    }

    [Fact]
    public void CountConstraint_Description_OnlyMin_ReturnsCorrectFormat()
    {
        var constraint = new CountConstraint(minCount: 2);
        Assert.Equal("count(min=2)", constraint.Description);
    }

    [Fact]
    public void CountConstraint_Description_OnlyMax_ReturnsCorrectFormat()
    {
        var constraint = new CountConstraint(maxCount: 10);
        Assert.Equal("count(max=10)", constraint.Description);
    }

    [Fact]
    public void CountConstraint_Description_NoConstraints_ReturnsCorrectFormat()
    {
        var constraint = new CountConstraint();
        Assert.Equal("count(any)", constraint.Description);
    }

    [Fact]
    public void ExactMatchConstraint_Constructor_ValidValue_Succeeds()
    {
        var constraint = new ExactMatchConstraint("test");
        Assert.Equal("test", constraint.ExpectedValue);
        Assert.False(constraint.IgnoreCase);
    }

    [Fact]
    public void ExactMatchConstraint_Constructor_WithIgnoreCase_Succeeds()
    {
        var constraint = new ExactMatchConstraint("test", ignoreCase: true);
        Assert.True(constraint.IgnoreCase);
    }

    [Fact]
    public void ExactMatchConstraint_Constructor_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ExactMatchConstraint(null!));
    }

    [Fact]
    public void ExactMatchConstraint_Validate_ExactMatch_ReturnsTrue()
    {
        var constraint = new ExactMatchConstraint("test");
        Assert.True(constraint.Validate("test"));
    }

    [Fact]
    public void ExactMatchConstraint_Validate_NoMatch_ReturnsFalse()
    {
        var constraint = new ExactMatchConstraint("test");
        Assert.False(constraint.Validate("Test"));
    }

    [Fact]
    public void ExactMatchConstraint_Validate_IgnoreCase_MatchesRegardlessOfCase()
    {
        var constraint = new ExactMatchConstraint("test", ignoreCase: true);
        Assert.True(constraint.Validate("TEST"));
        Assert.True(constraint.Validate("Test"));
        Assert.True(constraint.Validate("test"));
    }

    [Fact]
    public void ExactMatchConstraint_Validate_CaseSensitive_MatchesCaseSensitively()
    {
        var constraint = new ExactMatchConstraint("test", ignoreCase: false);
        Assert.True(constraint.Validate("test"));
        Assert.False(constraint.Validate("TEST"));
    }

    [Fact]
    public void ExactMatchConstraint_Description_ReturnsCorrectFormat()
    {
        var constraint = new ExactMatchConstraint("value");
        Assert.Equal("exact(\"value\")", constraint.Description);
    }
}
