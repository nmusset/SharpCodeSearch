namespace SharpCodeSearch.Models;

/// <summary>
/// Abstract base class for all pattern nodes.
/// </summary>
public abstract class PatternNode
{
    /// <summary>
    /// Position in the original pattern string.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Length in the original pattern string.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Accepts a visitor for pattern traversal.
    /// </summary>
    public abstract void Accept(IPatternVisitor visitor);
}

/// <summary>
/// Represents literal text in a pattern.
/// </summary>
public class TextNode : PatternNode
{
    public required string Text { get; init; }

    public override void Accept(IPatternVisitor visitor)
    {
        visitor.VisitText(this);
    }

    public override string ToString() => Text;
}

/// <summary>
/// Represents a placeholder in a pattern (e.g., $expr$, $var$).
/// </summary>
public class PlaceholderNode : PatternNode
{
    /// <summary>
    /// The name of the placeholder (without $).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The type of placeholder.
    /// </summary>
    public required PlaceholderType Type { get; init; }

    /// <summary>
    /// Optional constraints on the placeholder.
    /// </summary>
    public List<IConstraint> Constraints { get; init; } = new();

    public override void Accept(IPatternVisitor visitor)
    {
        visitor.VisitPlaceholder(this);
    }

    public override string ToString() => $"${Name}$";
}

/// <summary>
/// Represents a complete pattern as a sequence of nodes.
/// </summary>
public class PatternAst
{
    public List<PatternNode> Nodes { get; init; } = new();

    /// <summary>
    /// The original pattern string.
    /// </summary>
    public required string OriginalPattern { get; init; }

    public override string ToString() => OriginalPattern;
}

/// <summary>
/// Visitor interface for traversing pattern nodes.
/// </summary>
public interface IPatternVisitor
{
    void VisitText(TextNode node);
    void VisitPlaceholder(PlaceholderNode node);
}
