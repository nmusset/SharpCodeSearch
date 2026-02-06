using System.Text;

namespace SharpCodeSearch.Services;

/// <summary>
/// Parses search patterns into an AST.
/// </summary>
public class PatternParser
{
    /// <summary>
    /// Parses a pattern string into a PatternAst.
    /// </summary>
    /// <param name="pattern">The pattern string to parse</param>
    /// <returns>Parsed pattern AST</returns>
    public Models.PatternAst Parse(string pattern)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        var tokens = Tokenize(pattern);
        var nodes = new List<Models.PatternNode>();

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.Text)
            {
                nodes.Add(new Models.TextNode
                {
                    Text = token.Value,
                    Position = token.Position,
                    Length = token.Length
                });
            }
            else if (token.Type == TokenType.Placeholder)
            {
                nodes.Add(ParsePlaceholder(token));
            }
        }

        return new Models.PatternAst
        {
            OriginalPattern = pattern,
            Nodes = nodes
        };
    }

    /// <summary>
    /// Tokenizes a pattern string.
    /// </summary>
    /// <param name="pattern">The pattern to tokenize</param>
    /// <returns>List of tokens</returns>
    public List<Token> Tokenize(string pattern)
    {
        var tokens = new List<Token>();
        var position = 0;
        var currentText = new StringBuilder();
        var textStartPosition = 0;

        while (position < pattern.Length)
        {
            if (pattern[position] == '$')
            {
                // Save any accumulated text
                if (currentText.Length > 0)
                {
                    tokens.Add(new Token
                    {
                        Type = TokenType.Text,
                        Value = currentText.ToString(),
                        Position = textStartPosition,
                        Length = currentText.Length
                    });
                    currentText.Clear();
                }

                // Find the closing $
                var endPosition = pattern.IndexOf('$', position + 1);
                if (endPosition == -1)
                {
                    throw new PatternParseException($"Unclosed placeholder at position {position}", position);
                }

                var placeholderContent = pattern.Substring(position + 1, endPosition - position - 1);
                tokens.Add(new Token
                {
                    Type = TokenType.Placeholder,
                    Value = placeholderContent,
                    Position = position,
                    Length = endPosition - position + 1
                });

                position = endPosition + 1;
                textStartPosition = position;
            }
            else
            {
                if (currentText.Length == 0)
                {
                    textStartPosition = position;
                }
                currentText.Append(pattern[position]);
                position++;
            }
        }

        // Add any remaining text
        if (currentText.Length > 0)
        {
            tokens.Add(new Token
            {
                Type = TokenType.Text,
                Value = currentText.ToString(),
                Position = textStartPosition,
                Length = currentText.Length
            });
        }

        return tokens;
    }

    /// <summary>
    /// Validates a pattern string.
    /// </summary>
    /// <param name="pattern">The pattern to validate</param>
    /// <returns>Validation result</returns>
    public PatternValidationResult ValidatePattern(string pattern)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(pattern))
        {
            errors.Add("Pattern cannot be empty");
            return new PatternValidationResult { IsValid = false, Errors = errors };
        }

        try
        {
            var tokens = Tokenize(pattern);

            // Check for empty placeholders
            foreach (var token in tokens.Where(t => t.Type == TokenType.Placeholder))
            {
                if (string.IsNullOrWhiteSpace(token.Value))
                {
                    errors.Add($"Empty placeholder at position {token.Position}");
                }
            }

            // Try to parse
            var ast = Parse(pattern);

            // Validate placeholders
            foreach (var node in ast.Nodes.OfType<Models.PlaceholderNode>())
            {
                if (string.IsNullOrWhiteSpace(node.Name))
                {
                    errors.Add($"Invalid placeholder name at position {node.Position}");
                }
            }
        }
        catch (PatternParseException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected error: {ex.Message}");
        }

        return new PatternValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Parses a placeholder token into a PlaceholderNode.
    /// </summary>
    private Models.PlaceholderNode ParsePlaceholder(Token token)
    {
        var content = token.Value;

        // For now, simple parsing: just use the placeholder name
        // Later we can add constraint parsing like: $var:type(int)$
        var name = content.Trim();
        var type = InferPlaceholderType(name);

        return new Models.PlaceholderNode
        {
            Name = name,
            Type = type,
            Position = token.Position,
            Length = token.Length
        };
    }

    /// <summary>
    /// Infers the placeholder type from its name.
    /// </summary>
    private Models.PlaceholderType InferPlaceholderType(string name)
    {
        return name.ToLowerInvariant() switch
        {
            var n when n.Contains("expr") => Models.PlaceholderType.Expression,
            var n when n.Contains("stmt") => Models.PlaceholderType.Statement,
            var n when n.Contains("arg") => Models.PlaceholderType.Arguments,
            var n when n.Contains("type") => Models.PlaceholderType.Type,
            var n when n.Contains("member") => Models.PlaceholderType.Member,
            var n when n.Contains("var") || n.Contains("name") || n.Contains("id") => Models.PlaceholderType.Identifier,
            _ => Models.PlaceholderType.Any
        };
    }

    /// <summary>
    /// Parses a replacement pattern string into a ReplacePattern.
    /// </summary>
    /// <param name="replacePattern">The replacement pattern string to parse</param>
    /// <param name="searchPattern">The search pattern that this replacement is for</param>
    /// <returns>Parsed replacement pattern</returns>
    public Models.ReplacePattern ParseReplacePattern(string replacePattern, Models.PatternAst searchPattern)
    {
        if (replacePattern == null)
            throw new ArgumentNullException(nameof(replacePattern));
        if (searchPattern == null)
            throw new ArgumentNullException(nameof(searchPattern));

        // Get all placeholder names from search pattern
        var searchPlaceholders = searchPattern.Nodes
            .OfType<Models.PlaceholderNode>()
            .Select(p => p.Name)
            .ToHashSet();

        var tokens = Tokenize(replacePattern);
        var nodes = new List<Models.ReplacePatternNode>();

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.Text)
            {
                nodes.Add(new Models.ReplaceTextNode
                {
                    Text = token.Value,
                    Position = token.Position,
                    Length = token.Length
                });
            }
            else if (token.Type == TokenType.Placeholder)
            {
                var placeholderName = token.Value.Trim();

                // Validate that the placeholder exists in the search pattern
                if (!searchPlaceholders.Contains(placeholderName))
                {
                    throw new PatternParseException(
                        $"Placeholder '${placeholderName}$' in replacement pattern does not exist in search pattern. " +
                        $"Available placeholders: {string.Join(", ", searchPlaceholders.Select(p => $"${p}$"))}",
                        token.Position);
                }

                nodes.Add(new Models.ReplacePlaceholderNode
                {
                    Name = placeholderName,
                    Position = token.Position,
                    Length = token.Length
                });
            }
        }

        return new Models.ReplacePattern
        {
            OriginalPattern = replacePattern,
            SearchPattern = searchPattern,
            Nodes = nodes
        };
    }

    /// <summary>
    /// Validates a replacement pattern string against a search pattern.
    /// </summary>
    /// <param name="replacePattern">The replacement pattern to validate</param>
    /// <param name="searchPattern">The search pattern</param>
    /// <returns>Validation result</returns>
    public PatternValidationResult ValidateReplacePattern(string replacePattern, Models.PatternAst searchPattern)
    {
        var errors = new List<string>();

        if (searchPattern == null)
        {
            errors.Add("Search pattern cannot be null");
            return new PatternValidationResult { IsValid = false, Errors = errors };
        }

        if (string.IsNullOrWhiteSpace(replacePattern))
        {
            errors.Add("Replacement pattern cannot be empty");
            return new PatternValidationResult { IsValid = false, Errors = errors };
        }

        try
        {
            var parsed = ParseReplacePattern(replacePattern, searchPattern);
        }
        catch (PatternParseException ex)
        {
            errors.Add(ex.Message);
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected error: {ex.Message}");
        }

        return new PatternValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

/// <summary>
/// Represents a token in the pattern.
/// </summary>
public class Token
{
    public TokenType Type { get; init; }
    public required string Value { get; init; }
    public int Position { get; init; }
    public int Length { get; init; }
}

/// <summary>
/// Types of tokens.
/// </summary>
public enum TokenType
{
    Text,
    Placeholder
}

/// <summary>
/// Result of pattern validation.
/// </summary>
public class PatternValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Exception thrown when pattern parsing fails.
/// </summary>
public class PatternParseException : Exception
{
    public int Position { get; }

    public PatternParseException(string message, int position) : base(message)
    {
        Position = position;
    }
}
