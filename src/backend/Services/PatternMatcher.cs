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

        // Try composite pattern matching for method call patterns like "MethodName($args$)"
        if (patternIndex == 0 && IsMethodCallPattern(patternNodes))
        {
            return MatchMethodCallPattern(patternNodes, syntaxNode, placeholders);
        }

        // Try composite pattern matching for type patterns like "$type$ varName" or "List<$type$>"
        if (patternIndex == 0 && IsTypePattern(patternNodes))
        {
            return MatchTypePattern(patternNodes, syntaxNode, placeholders);
        }

        var patternNode = patternNodes[patternIndex];

        // Handle text nodes
        if (patternNode is TextNode textNode)
        {
            // For patterns that are ONLY text (no placeholders), use structural matching
            bool isTextOnlyPattern = patternNodes.Count == 1 && patternNodes[0] is TextNode;

            if (isTextOnlyPattern)
            {
                // Try to parse the text as C# code for structural matching
                var parsedPattern = TryParsePatternAsCode(textNode.Text);
                if (parsedPattern != null)
                {
                    // Structural match: compare syntax tree structures
                    if (AreNodesStructurallyEquivalent(parsedPattern, syntaxNode))
                    {
                        return true;
                    }
                    return false;
                }
            }

            // Fall back to text-based matching for:
            // 1. Patterns with mixed text/placeholders
            // 2. Text that can't be parsed as valid C# code
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
    /// Checks if the pattern looks like a method call with arguments: "MethodName($args$)"
    /// </summary>
    private bool IsMethodCallPattern(List<PatternNode> patternNodes)
    {
        // Pattern should have at least a text node and a placeholder
        if (patternNodes.Count < 2)
            return false;

        // Check if there's a text node followed by an argument placeholder
        for (int i = 0; i < patternNodes.Count - 1; i++)
        {
            if (patternNodes[i] is TextNode text &&
                patternNodes[i + 1] is PlaceholderNode ph &&
                ph.Type == PlaceholderType.Arguments &&
                text.Text.Contains("("))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Matches a method call pattern like "WriteLine($args$)" against an invocation expression.
    /// </summary>
    private bool MatchMethodCallPattern(
        List<PatternNode> patternNodes,
        SyntaxNode syntaxNode,
        Dictionary<string, string> placeholders)
    {
        // Handle constructor calls (new ClassName($args$))
        if (syntaxNode is ObjectCreationExpressionSyntax objectCreation)
        {
            return MatchConstructorPattern(patternNodes, objectCreation, placeholders);
        }

        // Must be an invocation expression for regular method calls
        if (syntaxNode is not InvocationExpressionSyntax invocation)
            return false;

        // Extract the method name from the pattern (text before the opening parenthesis)
        var textNode = patternNodes.OfType<TextNode>().FirstOrDefault();
        if (textNode == null)
            return false;

        var methodNamePattern = textNode.Text.Split('(')[0].Trim();

        // Get the method name from the invocation (full or partial)
        var methodName = ExtractMethodName(invocation.Expression);
        var fullMethodName = invocation.Expression.ToString();

        // Check if method name matches (try both full and partial names)
        if (!methodName.Contains(methodNamePattern) && !fullMethodName.Contains(methodNamePattern))
            return false;

        // Now match the argument placeholder against the argument list
        var argPlaceholder = patternNodes.OfType<PlaceholderNode>()
            .FirstOrDefault(p => p.Type == PlaceholderType.Arguments);

        if (argPlaceholder != null && invocation.ArgumentList != null)
        {
            // Extract arguments without parentheses
            var argsValue = ExtractArgumentsValue(invocation.ArgumentList);

            // Validate count constraints if present
            if (argPlaceholder.Constraints.OfType<CountConstraint>().Any())
            {
                var argCount = invocation.ArgumentList.Arguments.Count;
                foreach (var constraint in argPlaceholder.Constraints.OfType<CountConstraint>())
                {
                    if (constraint.MinCount.HasValue && argCount < constraint.MinCount.Value)
                        return false;
                    if (constraint.MaxCount.HasValue && argCount > constraint.MaxCount.Value)
                        return false;
                }
            }

            // Store the placeholder value
            placeholders[argPlaceholder.Name] = argsValue;
        }

        return true;
    }

    /// <summary>
    /// Matches a constructor pattern like "new MyClass($args$)".
    /// </summary>
    private bool MatchConstructorPattern(
        List<PatternNode> patternNodes,
        ObjectCreationExpressionSyntax objectCreation,
        Dictionary<string, string> placeholders)
    {
        // Extract the class name from the pattern
        var textNode = patternNodes.OfType<TextNode>().FirstOrDefault();
        if (textNode == null)
            return false;

        // Pattern should be like "new ClassName(" or just "ClassName("
        var pattern = textNode.Text.Replace("new", "").Trim().Split('(')[0].Trim();

        // Get the type name from the object creation
        var typeName = objectCreation.Type.ToString();

        // Check if type name matches
        if (!typeName.Contains(pattern))
            return false;

        // Match the argument placeholder
        var argPlaceholder = patternNodes.OfType<PlaceholderNode>()
            .FirstOrDefault(p => p.Type == PlaceholderType.Arguments);

        if (argPlaceholder != null && objectCreation.ArgumentList != null)
        {
            // Extract arguments without parentheses
            var argsValue = ExtractArgumentsValue(objectCreation.ArgumentList);

            // Validate count constraints
            if (argPlaceholder.Constraints.OfType<CountConstraint>().Any())
            {
                var argCount = objectCreation.ArgumentList.Arguments.Count;
                foreach (var constraint in argPlaceholder.Constraints.OfType<CountConstraint>())
                {
                    if (constraint.MinCount.HasValue && argCount < constraint.MinCount.Value)
                        return false;
                    if (constraint.MaxCount.HasValue && argCount > constraint.MaxCount.Value)
                        return false;
                }
            }

            placeholders[argPlaceholder.Name] = argsValue;
        }

        return true;
    }

    /// <summary>
    /// Extracts the method name from an invocation expression.
    /// Returns just the method name (e.g., "WriteLine" from "Console.WriteLine").
    /// </summary>
    private string ExtractMethodName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax ids => ids.Identifier.Text,
            MemberAccessExpressionSyntax mae => mae.Name.Identifier.Text,
            GenericNameSyntax gns => gns.Identifier.Text,
            _ => expression.ToString()
        };
    }

    /// <summary>
    /// Extracts argument values from an ArgumentListSyntax without parentheses.
    /// </summary>
    private string ExtractArgumentsValue(ArgumentListSyntax argumentList)
    {
        if (argumentList.Arguments.Count == 0)
            return string.Empty;

        // For single argument, return just the argument expression
        if (argumentList.Arguments.Count == 1)
            return argumentList.Arguments[0].ToString();

        // For multiple arguments, return comma-separated list
        return string.Join(", ", argumentList.Arguments.Select(a => a.ToString()));
    }

    /// <summary>
    /// Checks if the pattern contains a type placeholder with contextual text.
    /// Examples: "$type$ varName", "List<$type$>", "$type$[] array"
    /// </summary>
    private bool IsTypePattern(List<PatternNode> patternNodes)
    {
        // Must have at least one type placeholder
        return patternNodes.OfType<PlaceholderNode>()
            .Any(p => p.Type == PlaceholderType.Type);
    }

    /// <summary>
    /// Matches patterns containing type placeholders.
    /// Handles patterns like: "$type$ varName", "List<$type$>", "$type$[] array"
    /// </summary>
    private bool MatchTypePattern(
        List<PatternNode> patternNodes,
        SyntaxNode syntaxNode,
        Dictionary<string, string> placeholders)
    {
        // Try to match various declaration contexts
        return syntaxNode switch
        {
            VariableDeclarationSyntax varDecl => MatchTypeInVariableDeclaration(patternNodes, varDecl, placeholders),
            FieldDeclarationSyntax fieldDecl => MatchTypeInFieldDeclaration(patternNodes, fieldDecl, placeholders),
            ParameterSyntax parameter => MatchTypeInParameter(patternNodes, parameter, placeholders),
            PropertyDeclarationSyntax propDecl => MatchTypeInPropertyDeclaration(patternNodes, propDecl, placeholders),
            MethodDeclarationSyntax methodDecl => MatchTypeInMethodDeclaration(patternNodes, methodDecl, placeholders),
            CastExpressionSyntax castExpr => MatchTypeInCastExpression(patternNodes, castExpr, placeholders),
            _ => false
        };
    }

    /// <summary>
    /// Matches type pattern in variable declarations like "int x = 5;"
    /// </summary>
    private bool MatchTypeInVariableDeclaration(
        List<PatternNode> patternNodes,
        VariableDeclarationSyntax varDecl,
        Dictionary<string, string> placeholders)
    {
        // Pattern like "$type$ x" or "List<$type$> list"
        var typePlaceholder = patternNodes.OfType<PlaceholderNode>()
            .FirstOrDefault(p => p.Type == PlaceholderType.Type);

        if (typePlaceholder == null)
            return false;

        // Check if there's a text node with variable name
        var textNode = patternNodes.OfType<TextNode>().FirstOrDefault();
        if (textNode != null && varDecl.Variables.Count > 0)
        {
            // Check if any variable name matches the pattern
            var varName = textNode.Text.Trim();
            if (!varDecl.Variables.Any(v => v.Identifier.Text == varName))
                return false;
        }

        // Extract and store the type
        var typeValue = ExtractTypeValue(varDecl.Type);
        placeholders[typePlaceholder.Name] = typeValue;

        // Validate constraints
        if (!ValidateConstraints(typePlaceholder.Constraints, typeValue, varDecl.Type))
            return false;

        return true;
    }

    /// <summary>
    /// Matches type pattern in field declarations.
    /// </summary>
    private bool MatchTypeInFieldDeclaration(
        List<PatternNode> patternNodes,
        FieldDeclarationSyntax fieldDecl,
        Dictionary<string, string> placeholders)
    {
        var varDecl = fieldDecl.Declaration;
        return MatchTypeInVariableDeclaration(patternNodes, varDecl, placeholders);
    }

    /// <summary>
    /// Matches type pattern in parameters like "int value".
    /// </summary>
    private bool MatchTypeInParameter(
        List<PatternNode> patternNodes,
        ParameterSyntax parameter,
        Dictionary<string, string> placeholders)
    {
        var typePlaceholder = patternNodes.OfType<PlaceholderNode>()
            .FirstOrDefault(p => p.Type == PlaceholderType.Type);

        if (typePlaceholder == null || parameter.Type == null)
            return false;

        // Check if variable name matches pattern
        var textNode = patternNodes.OfType<TextNode>().FirstOrDefault();
        if (textNode != null)
        {
            var paramName = textNode.Text.Trim();
            if (parameter.Identifier.Text != paramName)
                return false;
        }

        var typeValue = ExtractTypeValue(parameter.Type);
        placeholders[typePlaceholder.Name] = typeValue;

        return ValidateConstraints(typePlaceholder.Constraints, typeValue, parameter.Type);
    }

    /// <summary>
    /// Matches type pattern in property declarations.
    /// </summary>
    private bool MatchTypeInPropertyDeclaration(
        List<PatternNode> patternNodes,
        PropertyDeclarationSyntax propDecl,
        Dictionary<string, string> placeholders)
    {
        var typePlaceholder = patternNodes.OfType<PlaceholderNode>()
            .FirstOrDefault(p => p.Type == PlaceholderType.Type);

        if (typePlaceholder == null)
            return false;

        // Check if property name matches pattern
        var textNode = patternNodes.OfType<TextNode>().FirstOrDefault();
        if (textNode != null)
        {
            var propName = textNode.Text.Replace("public", "").Replace("private", "")
                .Replace("protected", "").Replace("internal", "").Trim();
            if (!propDecl.Identifier.Text.Contains(propName) && !propName.Contains(propDecl.Identifier.Text))
                return false;
        }

        var typeValue = ExtractTypeValue(propDecl.Type);
        placeholders[typePlaceholder.Name] = typeValue;

        return ValidateConstraints(typePlaceholder.Constraints, typeValue, propDecl.Type);
    }

    /// <summary>
    /// Matches type pattern in method declarations (return type).
    /// </summary>
    private bool MatchTypeInMethodDeclaration(
        List<PatternNode> patternNodes,
        MethodDeclarationSyntax methodDecl,
        Dictionary<string, string> placeholders)
    {
        var typePlaceholder = patternNodes.OfType<PlaceholderNode>()
            .FirstOrDefault(p => p.Type == PlaceholderType.Type);

        if (typePlaceholder == null)
            return false;

        // Check if method name matches pattern
        var textNode = patternNodes.OfType<TextNode>().FirstOrDefault();
        if (textNode != null)
        {
            var methodName = textNode.Text.Replace("()", "").Trim();
            if (!methodDecl.Identifier.Text.Contains(methodName))
                return false;
        }

        var typeValue = ExtractTypeValue(methodDecl.ReturnType);
        placeholders[typePlaceholder.Name] = typeValue;

        return ValidateConstraints(typePlaceholder.Constraints, typeValue, methodDecl.ReturnType);
    }

    /// <summary>
    /// Matches type pattern in cast expressions like "(int)value".
    /// </summary>
    private bool MatchTypeInCastExpression(
        List<PatternNode> patternNodes,
        CastExpressionSyntax castExpr,
        Dictionary<string, string> placeholders)
    {
        var typePlaceholder = patternNodes.OfType<PlaceholderNode>()
            .FirstOrDefault(p => p.Type == PlaceholderType.Type);

        if (typePlaceholder == null)
            return false;

        // Check if the cast expression matches the pattern text
        var textNode = patternNodes.OfType<TextNode>().FirstOrDefault();
        if (textNode != null)
        {
            var exprText = castExpr.Expression.ToString();
            if (!textNode.Text.Contains(exprText))
                return false;
        }

        var typeValue = ExtractTypeValue(castExpr.Type);
        placeholders[typePlaceholder.Name] = typeValue;

        return ValidateConstraints(typePlaceholder.Constraints, typeValue, castExpr.Type);
    }

    /// <summary>
    /// Extracts the string representation of a type.
    /// Handles simple types, generics, arrays, nullables, etc.
    /// </summary>
    private string ExtractTypeValue(TypeSyntax type)
    {
        return type switch
        {
            // Simple type: int, string, MyClass
            PredefinedTypeSyntax predefined => predefined.Keyword.Text,

            // Identifier type: MyClass, var
            IdentifierNameSyntax identifier => identifier.Identifier.Text,

            // Qualified name: System.Int32
            QualifiedNameSyntax qualified => qualified.ToString(),

            // Generic type: List<int>, Dictionary<K,V>
            GenericNameSyntax generic => generic.Identifier.Text,

            // Array type: int[], string[,]
            ArrayTypeSyntax array => ExtractTypeValue(array.ElementType),

            // Nullable type: int?, string?
            NullableTypeSyntax nullable => ExtractTypeValue(nullable.ElementType),

            // Default: return full text
            _ => type.ToString()
        };
    }

    /// <summary>
    /// Tries to parse pattern text as C# code.
    /// Returns null if pattern is too simple or ambiguous to parse meaningfully.
    /// </summary>
    private SyntaxNode? TryParsePatternAsCode(string pattern)
    {
        try
        {
            // Don't try structural matching for simple identifiers or partial text
            // These should use text containment matching instead
            pattern = pattern.Trim();
            if (pattern.Split(' ').Length == 1 && !pattern.Contains(';') && !pattern.Contains('('))
            {
                // Single word without statement indicators - likely just a search term
                return null;
            }

            // Try parsing as statement
            var tree = CSharpSyntaxTree.ParseText(pattern);
            var root = tree.GetRoot();

            // Get the first statement or expression
            var statement = root.DescendantNodes().OfType<StatementSyntax>().FirstOrDefault();
            if (statement != null)
                return statement;

            var expression = root.DescendantNodes().OfType<ExpressionSyntax>().FirstOrDefault();
            if (expression != null)
                return expression;

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if two syntax nodes are structurally equivalent (same type and structure).
    /// </summary>
    private bool AreNodesStructurallyEquivalent(SyntaxNode pattern, SyntaxNode code)
    {
        // Must be same type
        if (pattern.GetType() != code.GetType())
            return false;

        // Compare normalized text (removes whitespace differences)
        var patternText = pattern.ToString().Trim().Replace(" ", "").Replace("\r", "").Replace("\n", "");
        var codeText = code.ToString().Trim().Replace(" ", "").Replace("\r", "").Replace("\n", "");

        return patternText == codeText;
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
            PlaceholderType.Statement => IsValidStatementNode(node),
            PlaceholderType.Arguments => node is ArgumentListSyntax,
            PlaceholderType.Type => node is TypeSyntax,
            PlaceholderType.Member => node is MemberAccessExpressionSyntax ||
                                     node is IdentifierNameSyntax,
            PlaceholderType.Any => true,
            _ => false
        };
    }

    /// <summary>
    /// Checks if a node is a valid statement for pattern matching.
    /// Excludes nodes that are technically statements but shouldn't match $stmt$ patterns.
    /// </summary>
    private bool IsValidStatementNode(SyntaxNode node)
    {
        // Must be a statement
        if (node is not StatementSyntax)
            return false;

        // Exclude certain statement types that are typically not what users want to match:

        // Exclude GlobalStatementSyntax (top-level statements wrapper)
        if (node is GlobalStatementSyntax)
            return false;

        // Exclude BlockSyntax - it's a statement but usually we want the statements INSIDE the block
        // If users want to match blocks specifically, they can use different patterns
        if (node is BlockSyntax)
            return false;

        // Exclude LocalFunctionStatementSyntax - it's a declaration, not an executable statement
        // Users looking for $stmt$ typically want executable code, not nested function declarations
        if (node is LocalFunctionStatementSyntax)
            return false;

        // We want to match actual executable statements like:
        // - ExpressionStatement (e.g., x++; Method();)
        // - ReturnStatement
        // - IfStatement, WhileStatement, ForStatement, etc.
        // - LocalDeclarationStatement (e.g., int x = 1;)
        // - ThrowStatement, TryStatement, etc.

        return true;
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
