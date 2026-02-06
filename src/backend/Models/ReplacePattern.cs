namespace SharpCodeSearch.Models;

/// <summary>
/// Abstract base class for replacement pattern nodes.
/// </summary>
public abstract class ReplacePatternNode
{
    /// <summary>
    /// Position in the original replacement pattern string.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Length in the original replacement pattern string.
    /// </summary>
    public int Length { get; set; }
}

/// <summary>
/// Represents literal text in a replacement pattern.
/// </summary>
public class ReplaceTextNode : ReplacePatternNode
{
    public required string Text { get; init; }

    public override string ToString() => Text;
}

/// <summary>
/// Represents a placeholder reference in a replacement pattern (e.g., $var$).
/// References a captured value from the search pattern.
/// </summary>
public class ReplacePlaceholderNode : ReplacePatternNode
{
    /// <summary>
    /// The name of the placeholder being referenced (without $).
    /// Must correspond to a placeholder in the search pattern.
    /// </summary>
    public required string Name { get; init; }

    public override string ToString() => $"${Name}$";
}

/// <summary>
/// Represents a complete replacement pattern.
/// </summary>
public class ReplacePattern
{
    /// <summary>
    /// The nodes that make up the replacement pattern.
    /// </summary>
    public List<ReplacePatternNode> Nodes { get; init; } = new();

    /// <summary>
    /// The original replacement pattern string.
    /// </summary>
    public required string OriginalPattern { get; init; }

    /// <summary>
    /// The search pattern this replacement is associated with.
    /// Used for validation that all referenced placeholders exist.
    /// </summary>
    public required PatternAst SearchPattern { get; init; }

    public override string ToString() => OriginalPattern;
}

/// <summary>
/// Represents the result of applying a replacement.
/// </summary>
public class ReplacementResult
{
    /// <summary>
    /// The replacement text that should replace the matched code.
    /// </summary>
    public required string ReplacementText { get; init; }

    /// <summary>
    /// The original matched text.
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Base indentation level of the match location (number of spaces/tabs).
    /// Used to properly indent multi-line replacements.
    /// </summary>
    public int BaseIndentation { get; init; }

    /// <summary>
    /// The file path where the match occurred.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// The start position of the match in the file.
    /// </summary>
    public int StartPosition { get; init; }

    /// <summary>
    /// The end position of the match in the file.
    /// </summary>
    public int EndPosition { get; init; }
}
