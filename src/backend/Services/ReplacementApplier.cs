using System.Text;

namespace SharpCodeSearch.Services;

/// <summary>
/// Applies replacements to source files.
/// </summary>
public class ReplacementApplier
{
    /// <summary>
    /// Applies a single replacement to a file.
    /// </summary>
    /// <param name="filePath">Path to the file to modify</param>
    /// <param name="replacement">The replacement to apply</param>
    /// <returns>Result of the operation</returns>
    public ReplacementApplicationResult ApplyReplacement(string filePath, Models.ReplacementResult replacement)
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));
        if (replacement == null)
            throw new ArgumentNullException(nameof(replacement));

        if (!File.Exists(filePath))
            return new ReplacementApplicationResult
            {
                Success = false,
                Error = $"File not found: {filePath}"
            };

        try
        {
            var content = File.ReadAllText(filePath, Encoding.UTF8);
            var modifiedContent = ApplyReplacementToContent(content, replacement);
            File.WriteAllText(filePath, modifiedContent, Encoding.UTF8);

            return new ReplacementApplicationResult
            {
                Success = true,
                FilePath = filePath,
                ReplacementsApplied = 1
            };
        }
        catch (Exception ex)
        {
            return new ReplacementApplicationResult
            {
                Success = false,
                Error = $"Error applying replacement: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Applies multiple replacements to files.
    /// Handles multiple replacements in the same file correctly by applying them in reverse position order.
    /// </summary>
    /// <param name="replacements">Replacements to apply, grouped by file</param>
    /// <returns>Results for each file</returns>
    public List<ReplacementApplicationResult> ApplyReplacements(List<Models.ReplacementResult> replacements)
    {
        if (replacements == null)
            throw new ArgumentNullException(nameof(replacements));

        var results = new List<ReplacementApplicationResult>();

        // Group by file path
        var byFile = replacements.GroupBy(r => r.FilePath).ToList();

        foreach (var fileGroup in byFile)
        {
            var filePath = fileGroup.Key;

            if (!File.Exists(filePath))
            {
                results.Add(new ReplacementApplicationResult
                {
                    Success = false,
                    FilePath = filePath,
                    Error = $"File not found: {filePath}"
                });
                continue;
            }

            try
            {
                var content = File.ReadAllText(filePath, Encoding.UTF8);

                // Sort by position descending (apply from end to start)
                // This prevents earlier replacements from invalidating later positions
                var sortedReplacements = fileGroup.OrderByDescending(r => r.StartPosition).ToList();

                foreach (var replacement in sortedReplacements)
                {
                    content = ApplyReplacementToContent(content, replacement);
                }

                File.WriteAllText(filePath, content, Encoding.UTF8);

                results.Add(new ReplacementApplicationResult
                {
                    Success = true,
                    FilePath = filePath,
                    ReplacementsApplied = sortedReplacements.Count
                });
            }
            catch (Exception ex)
            {
                results.Add(new ReplacementApplicationResult
                {
                    Success = false,
                    FilePath = filePath,
                    Error = $"Error applying replacements: {ex.Message}",
                    ReplacementsApplied = 0
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Applies a single replacement to file content string.
    /// </summary>
    private string ApplyReplacementToContent(string content, Models.ReplacementResult replacement)
    {
        // Replace the matched code with the replacement text
        var before = content.Substring(0, replacement.StartPosition);
        var after = content.Substring(replacement.EndPosition);

        return before + replacement.ReplacementText + after;
    }
}

/// <summary>
/// Result of applying a replacement to a file.
/// </summary>
public class ReplacementApplicationResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Path to the file that was modified.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Number of replacements applied (may be > 1 for batches).
    /// </summary>
    public int ReplacementsApplied { get; init; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? Error { get; init; }
}
