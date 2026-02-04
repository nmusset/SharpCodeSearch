using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using SharpCodeSearch.Models;
using SharpCodeSearch.Services;

using Xunit;

namespace SharpCodeSearch.Tests;

public class ConstraintValidatorTests
{
    [Fact]
    public void Constructor_WithoutSemanticModel_Succeeds()
    {
        var validator = new ConstraintValidator();
        Assert.NotNull(validator);
    }

    [Fact]
    public void Constructor_WithSemanticModel_Succeeds()
    {
        var tree = CSharpSyntaxTree.ParseText("var x = 1;");
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var semanticModel = compilation.GetSemanticModel(tree);

        var validator = new ConstraintValidator(semanticModel);
        Assert.NotNull(validator);
    }

    [Fact]
    public void Validate_NullConstraint_ThrowsArgumentNullException()
    {
        var validator = new ConstraintValidator();
        Assert.Throws<ArgumentNullException>(() =>
            validator.Validate(null!, "value"));
    }

    [Fact]
    public void ValidateAll_EmptyConstraints_ReturnsTrue()
    {
        var validator = new ConstraintValidator();
        var result = validator.ValidateAll(new List<IConstraint>(), "value");
        Assert.True(result);
    }

    [Fact]
    public void ValidateAll_NullConstraints_ReturnsTrue()
    {
        var validator = new ConstraintValidator();
        var result = validator.ValidateAll(null!, "value");
        Assert.True(result);
    }

    [Fact]
    public void Validate_RegexConstraint_ValidatesCorrectly()
    {
        var validator = new ConstraintValidator();
        var constraint = new RegexConstraint("^test.*");

        Assert.True(validator.Validate(constraint, "testValue", null));
        Assert.False(validator.Validate(constraint, "otherValue", null));
    }

    [Fact]
    public void Validate_ExactMatchConstraint_ValidatesCorrectly()
    {
        var validator = new ConstraintValidator();
        var constraint = new ExactMatchConstraint("test");

        Assert.True(validator.Validate(constraint, "test", null));
        Assert.False(validator.Validate(constraint, "Test", null));
    }

    [Fact]
    public void Validate_ExactMatchConstraint_IgnoreCase_ValidatesCorrectly()
    {
        var validator = new ConstraintValidator();
        var constraint = new ExactMatchConstraint("test", ignoreCase: true);

        Assert.True(validator.Validate(constraint, "test", null));
        Assert.True(validator.Validate(constraint, "TEST", null));
        Assert.True(validator.Validate(constraint, "Test", null));
    }

    [Fact]
    public void Validate_CountConstraint_AlwaysReturnsTrue()
    {
        var validator = new ConstraintValidator();
        var constraint = new CountConstraint(1, 5);

        // CountConstraint.Validate doesn't actually validate the count
        // It's validated separately during matching
        Assert.True(validator.Validate(constraint, "anything", null));
    }

    [Fact]
    public void ValidateTypeConstraint_WithoutSemanticModel_ReturnsTrue()
    {
        var validator = new ConstraintValidator();
        var constraint = new TypeConstraint("int");
        var code = "int x = 1;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var node = tree.GetRoot().DescendantNodes().First();

        // Without semantic model, optimistically returns true
        var result = validator.ValidateTypeConstraint(constraint, node);
        Assert.True(result);
    }

    [Fact]
    public void ValidateTypeConstraint_WithSemanticModel_ValidatesType()
    {
        var code = "int x = 1;";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create("test")
            .AddSyntaxTrees(tree)
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var semanticModel = compilation.GetSemanticModel(tree);
        var validator = new ConstraintValidator(semanticModel);

        var constraint = new TypeConstraint("int");
        var literalNode = tree.GetRoot().DescendantNodes()
            .First(n => n.ToString() == "1");

        var result = validator.ValidateTypeConstraint(constraint, literalNode);
        Assert.True(result);
    }

    [Fact]
    public void ValidateTypeConstraint_NullConstraint_ThrowsArgumentNullException()
    {
        var validator = new ConstraintValidator();
        var tree = CSharpSyntaxTree.ParseText("var x = 1;");
        var node = tree.GetRoot();

        Assert.Throws<ArgumentNullException>(() =>
            validator.ValidateTypeConstraint(null!, node));
    }

    [Fact]
    public void ValidateTypeConstraint_NullNode_ReturnsFalse()
    {
        var validator = new ConstraintValidator();
        var constraint = new TypeConstraint("int");

        var result = validator.ValidateTypeConstraint(constraint, null);
        Assert.False(result);
    }

    [Fact]
    public void ValidateRegexConstraint_NullConstraint_ThrowsArgumentNullException()
    {
        var validator = new ConstraintValidator();
        Assert.Throws<ArgumentNullException>(() =>
            validator.ValidateRegexConstraint(null!, "value"));
    }

    [Fact]
    public void ValidateRegexConstraint_ValidPattern_ReturnsTrue()
    {
        var validator = new ConstraintValidator();
        var constraint = new RegexConstraint("^[a-z]+$");

        Assert.True(validator.ValidateRegexConstraint(constraint, "test"));
        Assert.False(validator.ValidateRegexConstraint(constraint, "Test123"));
    }

    [Fact]
    public void ValidateCountConstraint_NullConstraint_ThrowsArgumentNullException()
    {
        var validator = new ConstraintValidator();
        Assert.Throws<ArgumentNullException>(() =>
            validator.ValidateCountConstraint(null!, 5));
    }

    [Fact]
    public void ValidateCountConstraint_WithinRange_ReturnsTrue()
    {
        var validator = new ConstraintValidator();
        var constraint = new CountConstraint(1, 5);

        Assert.True(validator.ValidateCountConstraint(constraint, 3));
        Assert.True(validator.ValidateCountConstraint(constraint, 1));
        Assert.True(validator.ValidateCountConstraint(constraint, 5));
    }

    [Fact]
    public void ValidateCountConstraint_BelowMin_ReturnsFalse()
    {
        var validator = new ConstraintValidator();
        var constraint = new CountConstraint(minCount: 2);

        Assert.False(validator.ValidateCountConstraint(constraint, 1));
        Assert.True(validator.ValidateCountConstraint(constraint, 2));
        Assert.True(validator.ValidateCountConstraint(constraint, 10));
    }

    [Fact]
    public void ValidateCountConstraint_AboveMax_ReturnsFalse()
    {
        var validator = new ConstraintValidator();
        var constraint = new CountConstraint(maxCount: 5);

        Assert.True(validator.ValidateCountConstraint(constraint, 3));
        Assert.True(validator.ValidateCountConstraint(constraint, 5));
        Assert.False(validator.ValidateCountConstraint(constraint, 6));
    }

    [Fact]
    public void ValidateCountConstraint_NoLimits_AlwaysReturnsTrue()
    {
        var validator = new ConstraintValidator();
        var constraint = new CountConstraint();

        Assert.True(validator.ValidateCountConstraint(constraint, 0));
        Assert.True(validator.ValidateCountConstraint(constraint, 100));
    }

    [Fact]
    public void ValidateExactMatchConstraint_NullConstraint_ThrowsArgumentNullException()
    {
        var validator = new ConstraintValidator();
        Assert.Throws<ArgumentNullException>(() =>
            validator.ValidateExactMatchConstraint(null!, "value"));
    }

    [Fact]
    public void ValidateExactMatchConstraint_CaseSensitive_ValidatesCorrectly()
    {
        var validator = new ConstraintValidator();
        var constraint = new ExactMatchConstraint("Test", ignoreCase: false);

        Assert.True(validator.ValidateExactMatchConstraint(constraint, "Test"));
        Assert.False(validator.ValidateExactMatchConstraint(constraint, "test"));
    }

    [Fact]
    public void ValidateExactMatchConstraint_NullValue_HandlesSafely()
    {
        var validator = new ConstraintValidator();
        var constraint = new ExactMatchConstraint("test");

        Assert.False(validator.ValidateExactMatchConstraint(constraint, null!));
    }

    [Fact]
    public void ValidateAll_MultipleConstraints_ValidatesAll()
    {
        var validator = new ConstraintValidator();
        var constraints = new List<IConstraint>
        {
            new RegexConstraint("^test.*"),
            new ExactMatchConstraint("testValue")
        };

        Assert.True(validator.ValidateAll(constraints, "testValue"));
        Assert.False(validator.ValidateAll(constraints, "testOther"));
        Assert.False(validator.ValidateAll(constraints, "other"));
    }

    [Fact]
    public void ValidateAll_FirstConstraintFails_ReturnsFalse()
    {
        var validator = new ConstraintValidator();
        var constraints = new List<IConstraint>
        {
            new RegexConstraint("^test.*"),
            new RegexConstraint(".*Value$")
        };

        Assert.False(validator.ValidateAll(constraints, "otherValue"));
    }

    [Fact]
    public void ValidateAll_LastConstraintFails_ReturnsFalse()
    {
        var validator = new ConstraintValidator();
        var constraints = new List<IConstraint>
        {
            new RegexConstraint("^test.*"),
            new RegexConstraint(".*Other$")
        };

        Assert.False(validator.ValidateAll(constraints, "testValue"));
    }
}
