using Microsoft.CodeAnalysis;

using SharpCodeSearch.Models;

namespace SharpCodeSearch.Services;

/// <summary>
/// Validates constraints against matched values and syntax nodes.
/// </summary>
public class ConstraintValidator
{
    private readonly SemanticModel? _semanticModel;

    public ConstraintValidator(SemanticModel? semanticModel = null)
    {
        _semanticModel = semanticModel;
    }

    /// <summary>
    /// Validates all constraints for a value.
    /// </summary>
    /// <param name="constraints">List of constraints to validate</param>
    /// <param name="value">The extracted placeholder value</param>
    /// <param name="node">The matched syntax node</param>
    /// <returns>True if all constraints pass</returns>
    public bool ValidateAll(
        List<IConstraint> constraints,
        string value,
        SyntaxNode? node = null)
    {
        if (constraints == null || !constraints.Any())
            return true;

        foreach (var constraint in constraints)
        {
            if (!Validate(constraint, value, node))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Validates a single constraint.
    /// </summary>
    /// <param name="constraint">The constraint to validate</param>
    /// <param name="value">The extracted placeholder value</param>
    /// <param name="node">The matched syntax node (required for type constraints)</param>
    /// <returns>True if constraint passes</returns>
    public bool Validate(
        IConstraint constraint,
        string value,
        SyntaxNode? node = null)
    {
        if (constraint == null)
            throw new ArgumentNullException(nameof(constraint));

        return constraint switch
        {
            TypeConstraint typeConstraint => ValidateTypeConstraint(typeConstraint, node),
            RegexConstraint regexConstraint => regexConstraint.Validate(value),
            CountConstraint countConstraint => countConstraint.Validate(value),
            ExactMatchConstraint exactMatch => exactMatch.Validate(value),
            _ => constraint.Validate(value)
        };
    }

    /// <summary>
    /// Validates a type constraint using semantic analysis.
    /// </summary>
    /// <param name="constraint">The type constraint</param>
    /// <param name="node">The syntax node to check</param>
    /// <returns>True if the node's type matches the constraint</returns>
    public bool ValidateTypeConstraint(TypeConstraint constraint, SyntaxNode? node)
    {
        if (constraint == null)
            throw new ArgumentNullException(nameof(constraint));

        if (node == null)
            return false;

        if (_semanticModel == null)
        {
            // Without semantic model, we can't validate type constraints
            // Return true to allow matching (optimistic)
            return true;
        }

        try
        {
            var typeInfo = _semanticModel.GetTypeInfo(node);
            if (typeInfo.Type == null)
                return false;

            // Check both full name and simple name
            var fullTypeName = typeInfo.Type.ToDisplayString();
            var simpleTypeName = typeInfo.Type.Name;

            return fullTypeName == constraint.TypeName ||
                   simpleTypeName == constraint.TypeName ||
                   fullTypeName.EndsWith("." + constraint.TypeName);
        }
        catch
        {
            // If we can't get type info, allow the match
            return true;
        }
    }

    /// <summary>
    /// Validates a regex constraint.
    /// </summary>
    public bool ValidateRegexConstraint(RegexConstraint constraint, string value)
    {
        if (constraint == null)
            throw new ArgumentNullException(nameof(constraint));

        return constraint.Validate(value ?? string.Empty);
    }

    /// <summary>
    /// Validates a count constraint (used for argument lists, etc.).
    /// </summary>
    /// <param name="constraint">The count constraint</param>
    /// <param name="count">The actual count</param>
    /// <returns>True if count is within constraints</returns>
    public bool ValidateCountConstraint(CountConstraint constraint, int count)
    {
        if (constraint == null)
            throw new ArgumentNullException(nameof(constraint));

        if (constraint.MinCount.HasValue && count < constraint.MinCount.Value)
            return false;

        if (constraint.MaxCount.HasValue && count > constraint.MaxCount.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Validates an exact match constraint.
    /// </summary>
    public bool ValidateExactMatchConstraint(ExactMatchConstraint constraint, string value)
    {
        if (constraint == null)
            throw new ArgumentNullException(nameof(constraint));

        return constraint.Validate(value ?? string.Empty);
    }
}
