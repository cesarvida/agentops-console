using System.Text.RegularExpressions;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Application.Analysis.Pipeline;

/// <summary>
/// Layer 1 — Safely extracts structured content from .md and .py files
/// without executing any code.
/// </summary>
public class SafeContentExtractor
{
    // Markdown code block: ```...```
    private static readonly Regex MarkdownCodeBlock =
        new(@"```[\w]*\r?\n(.*?)```", RegexOptions.Singleline | RegexOptions.Compiled);

    // HTML comments
    private static readonly Regex HtmlComment =
        new(@"<!--(.*?)-->", RegexOptions.Singleline | RegexOptions.Compiled);

    // URLs (http/https)
    private static readonly Regex UrlPattern =
        new(@"https?://[^\s\)\]>""']+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // YAML frontmatter
    private static readonly Regex FrontmatterPattern =
        new(@"^---\r?\n(.*?)\r?\n---", RegexOptions.Singleline | RegexOptions.Compiled);

    // Python string literals (single/double quoted, incl. triple-quoted)
    private static readonly Regex PyTripleQuote =
        new(@"(?:""""""(.*?)"""""")|(?:'''(.*?)''')", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex PySingleQuote =
        new(@"(?:""([^""\\]*(\\.[^""\\]*)*)"")|(?:'([^'\\]*(\\.[^'\\]*)*)')", RegexOptions.Compiled);

    // Python imports
    private static readonly Regex PyImport =
        new(@"^\s*(?:import|from)\s+\S+", RegexOptions.Multiline | RegexOptions.Compiled);

    // Hidden text: 20+ consecutive spaces
    private static readonly Regex HiddenSpaces =
        new(@" {20,}", RegexOptions.Compiled);

    // Zero-width chars / invisible unicode
    private static readonly Regex InvisibleChars =
        new(@"[\u200B\u200C\u200D\uFEFF\u00AD]", RegexOptions.Compiled);

    public ExtractedContent Extract(string filePath, string content)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var fileType = ext switch
        {
            ".md" or ".markdown" => "markdown",
            ".py"                => "python",
            _                    => DetectTypeFromContent(content)
        };

        var result = new ExtractedContent
        {
            FileType  = fileType,
            RawText   = content,
        };

        // Check for hidden text (≥20 spaces or invisible chars)
        result.HasHiddenText = HiddenSpaces.IsMatch(content) || InvisibleChars.IsMatch(content);

        if (fileType == "markdown")
            ExtractMarkdown(content, result);
        else
            ExtractPython(content, result);

        return result;
    }

    private static void ExtractMarkdown(string content, ExtractedContent result)
    {
        // Frontmatter
        var fm = FrontmatterPattern.Match(content);
        if (fm.Success)
            result.Metadata["frontmatter"] = fm.Groups[1].Value;

        // Code blocks
        foreach (Match m in MarkdownCodeBlock.Matches(content))
            result.CodeBlocks.Add(m.Groups[1].Value.Trim());

        // HTML comments — these are prime injection vectors
        foreach (Match m in HtmlComment.Matches(content))
            result.CodeBlocks.Add("<!-- " + m.Groups[1].Value.Trim() + " -->");

        // URLs
        foreach (Match m in UrlPattern.Matches(content))
            result.Urls.Add(m.Value);
    }

    private static void ExtractPython(string content, ExtractedContent result)
    {
        // Triple-quoted strings
        foreach (Match m in PyTripleQuote.Matches(content))
        {
            var val = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            if (!string.IsNullOrWhiteSpace(val))
                result.InlineStrings.Add(val.Trim());
        }

        // Single-line strings
        foreach (Match m in PySingleQuote.Matches(content))
        {
            var val = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[3].Value;
            if (!string.IsNullOrWhiteSpace(val) && val.Length > 3)
                result.InlineStrings.Add(val);
        }

        // Import statements
        foreach (Match m in PyImport.Matches(content))
            result.Metadata["import_" + result.Metadata.Count] = m.Value.Trim();

        // Inline URLs in strings
        foreach (Match m in UrlPattern.Matches(content))
            result.Urls.Add(m.Value);
    }

    private static string DetectTypeFromContent(string content)
    {
        if (content.Contains("import ") || content.Contains("def ") || content.Contains("print("))
            return "python";
        return "markdown";
    }
}
