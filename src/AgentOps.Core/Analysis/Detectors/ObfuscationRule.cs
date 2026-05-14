using System.Text.RegularExpressions;
using AgentOps.Core.Analysis;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Core.Analysis.Detectors;

/// <summary>
/// OB-001 — Detects obfuscation techniques used to hide malicious content.
/// Supports: markdown and python.
/// </summary>
public class ObfuscationRule : IPromptDetector
{
    public string DetectorName => "ObfuscationRule";
    public string[] SupportedTypes => ["markdown", "python"];

    // Long base64 strings (>50 chars) in markdown
    private static readonly Regex LongBase64InMarkdown =
        new(@"[A-Za-z0-9+/]{50,}={0,2}", RegexOptions.Compiled);

    // Unicode escape sequences in Python
    private static readonly Regex UnicodeEscapePython =
        new(@"\\u[0-9a-fA-F]{4}", RegexOptions.Compiled);

    // String concatenation forming dangerous keywords
    private static readonly Regex DangerousConcatenation =
        new(@"""([^""]{1,15})""\s*\+\s*""([^""]{1,15})""", RegexOptions.Compiled);

    // Hex string encoding
    private static readonly Regex HexEncoding =
        new(@"\\x[0-9a-fA-F]{2}(?:\\x[0-9a-fA-F]{2}){4,}", RegexOptions.Compiled);

    private static readonly string[] DangerousKeywords =
        ["os.remove", "os.unlink", "eval(", "exec(", "subprocess", "requests.post",
         "pickle.loads", "shutil.rmtree", "__import__"];

    public List<Finding> Analyze(ExtractedContent content, ContentContext context)
    {
        var findings = new List<Finding>();

        // 1. HasObfuscation from normalizer (high confidence — decoded text had dangerous keywords)
        if (content.HasObfuscation)
        {
            findings.Add(new Finding
            {
                RuleId          = "OB-001",
                RuleName        = DetectorName,
                Category        = FindingCategory.Obfuscation,
                Severity        = "CRITICAL",
                ConfidenceScore = 0.97f,
                Evidence        = "Obfuscation detected and decoded by ContentNormalizer",
                Explanation     = "Content contains obfuscated dangerous instructions (base64/unicode/concat) that decode to malicious patterns",
                Recommendation  = "Reject this file immediately — obfuscation is almost always malicious."
            });
        }

        // 2. Long base64 in markdown (HIGH — could be obfuscation)
        if (content.FileType == "markdown")
        {
            foreach (Match m in LongBase64InMarkdown.Matches(content.RawText))
            {
                findings.Add(new Finding
                {
                    RuleId          = "OB-001",
                    RuleName        = DetectorName,
                    Category        = FindingCategory.Obfuscation,
                    Severity        = "HIGH",
                    ConfidenceScore = 0.75f,
                    Evidence        = m.Value[..Math.Min(60, m.Value.Length)] + (m.Value.Length > 60 ? "..." : ""),
                    LineNumber      = FindLineNumber(content.RawText, m.Value[..20]),
                    Explanation     = "Long base64 string in markdown — may contain obfuscated instructions",
                    Recommendation  = "Decode and verify this base64 string before passing to LLM."
                });
                break; // Report once
            }
        }

        // 3. Unicode escapes in Python (HIGH)
        if (content.FileType == "python")
        {
            var unicodeMatches = UnicodeEscapePython.Matches(content.RawText);
            if (unicodeMatches.Count >= 3) // Multiple = suspicious
            {
                findings.Add(new Finding
                {
                    RuleId          = "OB-001",
                    RuleName        = DetectorName,
                    Category        = FindingCategory.Obfuscation,
                    Severity        = "HIGH",
                    ConfidenceScore = 0.78f,
                    Evidence        = $"{unicodeMatches.Count} unicode escape sequences detected",
                    Explanation     = "Multiple unicode escapes in Python code — possible keyword obfuscation",
                    Recommendation  = "Review unicode escapes and ensure they don't encode malicious patterns."
                });
            }
        }

        // 4. Dangerous concatenations
        foreach (Match m in DangerousConcatenation.Matches(content.RawText))
        {
            var combined = m.Groups[1].Value + m.Groups[2].Value;
            if (DangerousKeywords.Any(kw => kw.Contains(combined, StringComparison.OrdinalIgnoreCase) ||
                                            combined.Contains(kw[..Math.Min(6, kw.Length)], StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add(new Finding
                {
                    RuleId          = "OB-001",
                    RuleName        = DetectorName,
                    Category        = FindingCategory.Obfuscation,
                    Severity        = "HIGH",
                    ConfidenceScore = 0.88f,
                    Evidence        = m.Value,
                    LineNumber      = FindLineNumber(content.RawText, m.Value),
                    Explanation     = $"String concatenation forms dangerous keyword: '{combined}'",
                    Recommendation  = "This is obfuscated code — reject this file."
                });
            }
        }

        // 5. Hex encoding (HIGH)
        if (HexEncoding.IsMatch(content.RawText))
        {
            var m = HexEncoding.Match(content.RawText);
            findings.Add(new Finding
            {
                RuleId          = "OB-001",
                RuleName        = DetectorName,
                Category        = FindingCategory.Obfuscation,
                Severity        = "HIGH",
                ConfidenceScore = 0.80f,
                Evidence        = m.Value[..Math.Min(60, m.Value.Length)],
                Explanation     = "Hex-encoded string sequences — potential command obfuscation",
                Recommendation  = "Decode and inspect hex sequences before use."
            });
        }

        return findings;
    }

    private static int FindLineNumber(string text, string pattern)
    {
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return i + 1;
        }
        return -1;
    }
}
