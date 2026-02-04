using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SharpCodeSearch.Models;

namespace SharpCodeSearch.Services;

/// <summary>
/// Matches patterns against C# syntax nodes.
/// </summary>
public class PatternMatcher
{
    private readonly SemanticModel? _semanticModel;

    public PatternMatcher(SemanticModel? semanticModel = null)
    {
        _semanticModel = semanticModel;
    }

    /// <summary>
    /// Finds all matches of a pattern in a syntax tree.
    /// </summary>
    /// <param name="pattern">The pattern AST to match</param>
    /// <param name="syntaxNode">The root syntax node to search</param>
    /// <returns>List of matches with extracted placeholder values</returns>
    public List<PatternMatch> FindMatches(PatternAst pattern, SyntaxNode syntaxNode)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));
        if (syntaxNode == null)
            throw new ArgumentNullException(nameof(syntaxNode));

        var matches = new List<PatternMatch>();

        // Traverse all nodes in the syntax tree
        foreach (var node in Roslyn.RoslynHelper.EnumerateNodes(syntaxNode))
        {
            var placeholders = new Dictionary<string, string>();
            if (MatchNode(pattern.Nodes, 0, node, placeholders))
            {
                matches.Add(new PatternMatch
                {
                    Node = node,
                    Placeholders = placeholders,
                    Location = node.GetLocation()
                });
            }
        }

        return matches;
    }

    /// <summary>
    /// Recursively matches pattern nodes against syntax nodes.
    /// </summary>
    /// <param name="patternNodes">List of pattern nodes to match</param>
    /// <param name="patternIndex">Current index in pattern nodes</param>
    /// <param name="syntaxNode">The syntax node to match against</param>
    /// <param name="placeholders">Dictionary to store matched placeholder values</param>
    /// <returns>True if match successful</returns>
    private bool MatchNode(
        List<PatternNode> patternNodes,
        int patternIndex,
        SyntaxNode syntaxNode,
        Dictionary<string, string> placeholders)
    {
        // Base case: matched all pattern nodes
        if (patternIndex >= patternNodes.Count)
            return true;

        var patternNode = patternNodes[patternIndex];

        // Handle text nodes
        if (patternNode is TextNode textNode)
        {
            // Text nodes match against the syntax node's text
            var nodeText = syntaxNode.ToString();
            if (nodeText.Contains(textNode.Text))
            {
                // Continue matching with next pattern node
                return MatchNode(patternNodes, patternIndex + 1, syntaxNode, placeholders);
            }
            return false;
        }

        // Handle placeholder nodes
        if (patternNode is PlaceholderNode placeholder)
        {
            // For single-placeholder patterns, match directly
            if (patternNodes.Count == 1)
            {
                return MatchPlaceholder(placeholder, patternNodes, patternIndex, syntaxNode, placeholders);
            }
            else
            {
                // For multi-node patterns, we need structural matching
                // This is a simplified version - full implementation would need more sophisticated matching
                return MatchPlaceholder(placeholder, patternNodes, patternIndex, syntaxNode, placeholders);
            }
        }

        return false;
    }

    /// <summary>
    /// Matches a placeholder against a syntax node.
    /// </summary>
    private bool MatchPlaceholder(
        PlaceholderNode placeholder,
        List<PatternNode> patternNodes,
        int patternIndex,
        SyntaxNode syntaxNode,
        Dictionary<string, string> placeholders)
    {
        // First check if the node type is compatible with the placeholder type
        if (!IsPlaceholderTypeCompatible(placeholder.Type, syntaxNode))
            return false;

        // Extract the value to store
        var value = ExtractPlaceholderValue(syntaxNode, placeholder.Type);

        // Validate constraints BEFORE storing
        if (!ValidateConstraints(placeholder.Constraints, value, syntaxNode))
            return false;

        // Store or verify placeholder value
        if (placeholders.ContainsKey(placeholder.Name))
        {
            // If placeholder already matched, verify it's the same value
            if (placeholders[placeholder.Name] != value)
                return false;
        }
        else
        {
            placeholders[placeholder.Name] = value;
        }

        // Continue with next pattern node
        return MatchNode(patternNodes, patternIndex + 1, syntaxNode, placeholders);
    }

    /// <summary>
    /// Checks if a placeholder type is compatible with a syntax node.
    /// </summary>
    private bool IsPlaceholderTypeCompatible(PlaceholderType type, SyntaxNode node)
    {
        return type switch
        {
            PlaceholderType.Expression => node is ExpressionSyntax,
            PlaceholderType.Identifier => node is IdentifierNameSyntax ||
                                         node is VariableDeclaratorSyntax ||
                                         node is ParameterSyntax,
            PlaceholderType.Statement => node is StatementSyntax,
            PlaceholderType.Arguments => node is ArgumentListSyntax,
            PlaceholderType.Type => node is TypeSyntax,
            PlaceholderType.Member => node is MemberAccessExpressionSyntax ||
                                     node is IdentifierNameSyntax,
            PlaceholderType.Any => true,
            _ => false
        };
    }

    /// <summary>
    /// Extracts the value to store for a matched placeholder.
    /// </summary>
    private string ExtractPlaceholderValue(SyntaxNode node, PlaceholderType type)
    {
        // For identifiers, extract the actual identifier text
        if (node is IdentifierNameSyntax identifier)
        {
            return identifier.Identifier.Text;
        }

        // For variable declarators, extract the variable name
        if (node is VariableDeclaratorSyntax declarator)
        {
            return declarator.Identifier.Text;
        }

        // For parameters, extract the parameter name
        if (node is ParameterSyntax parameter)
        {
            return parameter.Identifier.Text;
        }

        // For member access, extract the member name
        if (node is MemberAccessExpressionSyntax member)
        {
            return member.Name.Identifier.Text;
        }

        // For other types, return the full text
        return node.ToString();
    }

    /// <summary>
    /// Validates all constraints for a matched placeholder.
    /// </summary>
    private bool ValidateConstraints(
        List<IConstraint> constraints,
        string value,
        SyntaxNode node)
    {
        foreach (var constraint in constraints)
        {
            if (constraint is TypeConstraint typeConstraint)
            {
                if (!ValidateTypeConstraint(typeConstraint, node))
                    return false;
            }
            else
            {
                if (!constraint.Validate(value))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Validates a type constraint using semantic analysis.
    /// </summary>
    private bool ValidateTypeConstraint(TypeConstraint constraint, SyntaxNode node)
    {
        if (_semanticModel == null)
            return true; // Can't validate without semantic model

        var typeInfo = _semanticModel.GetTypeInfo(node);
        if (typeInfo.Type == null)
            return false;

        var typeName = typeInfo.Type.ToDisplayString();
        return typeName == constraint.TypeName ||
               typeInfo.Type.Name == constraint.TypeName;
    }
}

/// <summary>
/// Represents a match result.
/// </summary>
public class PatternMatch
{
    /// <summary>
    /// The matched syntax node.
    /// </summary>
    public required SyntaxNode Node { get; init; }

    /// <summary>
    /// Extracted placeholder values.
    /// </summary>
    public Dictionary<string, string> Placeholders { get; init; } = new();

    /// <summary>
    /// Location of the match in source code.
    /// </summary>
    public required Location Location { get; init; }
}
