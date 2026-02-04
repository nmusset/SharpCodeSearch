using System.Text.RegularExpressions;

namespace SharpCodeSearch.Models;

/// <summary>
/// Types of placeholders supported in patterns.
/// </summary>
public enum PlaceholderType
{
    /// <summary>
    /// Expression placeholder (e.g., $expr$).
    /// </summary>
    Expression,

    /// <summary>
    /// Identifier placeholder (e.g., $var$, $name$).
    /// </summary>
    Identifier,

    /// <summary>
    /// Statement placeholder (e.g., $stmt$).
    /// </summary>
    Statement,

    /// <summary>
    /// Argument list placeholder (e.g., $args$).
    /// </summary>
    Arguments,

    /// <summary>
    /// Type placeholder (e.g., $type$).
    /// </summary>
    Type,

    /// <summary>
    /// Member placeholder (e.g., $member$).
    /// </summary>
    Member,

    /// <summary>
    /// Generic/wildcard placeholder.
    /// </summary>
    Any
}

/// <summary>
/// Base interface for all constraints.
/// </summary>
public interface IConstraint
{
    /// <summary>
    /// Validates if the given value satisfies this constraint.
    /// </summary>
    bool Validate(string value);

    /// <summary>
    /// Description of the constraint for error messages.
    /// </summary>
    string Description { get; }
}

/// <summary>
/// Constraint that matches against a regular expression.
/// </summary>
public class RegexConstraint : IConstraint
{
    private readonly Regex _regex;

    public string Pattern { get; }

    public RegexConstraint(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        Pattern = pattern;
        _regex = new Regex(pattern, RegexOptions.Compiled);
    }

    public bool Validate(string value)
    {
        return _regex.IsMatch(value ?? string.Empty);
    }

    public string Description => $"regex({Pattern})";
}

/// <summary>
/// Constraint that checks if a value matches a specific type.
/// </summary>
public class TypeConstraint : IConstraint
{
    public string TypeName { get; }

    public TypeConstraint(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("Type name cannot be null or empty", nameof(typeName));

        TypeName = typeName;
    }

    public bool Validate(string value)
    {
        // This will be validated against semantic model during matching
        // For now, just check it's not empty
        return !string.IsNullOrWhiteSpace(value);
    }

    public string Description => $"type({TypeName})";
}

/// <summary>
/// Constraint that checks the count of matched items (e.g., for argument lists).
/// </summary>
public class CountConstraint : IConstraint
{
    public int? MinCount { get; }
    public int? MaxCount { get; }

    public CountConstraint(int? minCount = null, int? maxCount = null)
    {
        if (minCount.HasValue && maxCount.HasValue && minCount > maxCount)
            throw new ArgumentException("MinCount cannot be greater than MaxCount");

        MinCount = minCount;
        MaxCount = maxCount;
    }

    public bool Validate(string value)
    {
        // Count validation happens during matching, not on string value
        // This is just a placeholder
        return true;
    }

    public string Description
    {
        get
        {
            if (MinCount.HasValue && MaxCount.HasValue)
                return $"count({MinCount}-{MaxCount})";
            if (MinCount.HasValue)
                return $"count(min={MinCount})";
            if (MaxCount.HasValue)
                return $"count(max={MaxCount})";
            return "count(any)";
        }
    }
}

/// <summary>
/// Constraint that checks for exact text match.
/// </summary>
public class ExactMatchConstraint : IConstraint
{
    public string ExpectedValue { get; }
    public bool IgnoreCase { get; }

    public ExactMatchConstraint(string expectedValue, bool ignoreCase = false)
    {
        ExpectedValue = expectedValue ?? throw new ArgumentNullException(nameof(expectedValue));
        IgnoreCase = ignoreCase;
    }

    public bool Validate(string value)
    {
        if (IgnoreCase)
            return string.Equals(ExpectedValue, value, StringComparison.OrdinalIgnoreCase);
        
        return string.Equals(ExpectedValue, value, StringComparison.Ordinal);
    }

    public string Description => $"exact(\"{ExpectedValue}\")";
}
