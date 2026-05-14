using System.Text.RegularExpressions;
using AgentOps.Core.Analysis;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Core.Analysis.Detectors;

/// <summary>
/// HI-001 — Detects hidden instructions (whitespace injection, CSS tricks, HTML comment instructions).
/// Supports: markdown and python.
/// </summary>
public class HiddenInstructionRule : IPromptDetector
{
    public string DetectorName => "HiddenInstructionRule";
    public string[] SupportedTypes => ["markdown", "python"];

    // CSS white-on-white trick
    private static readonly Regex WhiteTextCss =
        new(@"color\s*:\s*(white|#fff|#ffffff|#FFF|#FFFFFF)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // HTML comments containing action verbs
    private static readonly Regex ActionableHtmlComment =
        new(@"<!--\s*(?:ignore|system|override|act as|you are|your instructions|disregard|forget|new task|execute|run)\s",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    // Many blank lines (≥10) before some content (whitespace injection)
    private static readonly Regex BlankLineInjection =
        new(@"(\r?\n\s*){10,}\S", RegexOptions.Compiled);

    // Obvious hidden-text markers
    private static readonly string[] HiddenMarkers =
        ["display:none", "visibility:hidden", "font-size:0", "opacity:0", "color:transparent"];

    public List<Finding> Analyze(ExtractedContent content, ContentContext context)
    {
        var findings = new List<Finding>();
        var text = content.RawText;

        // 1. HasHiddenText flag from extractor (≥20 spaces / invisible chars)
        if (content.HasHiddenText)
        {
            findings.Add(new Finding
            {
                RuleId          = "HI-001",
                RuleName        = DetectorName,
                Category        = FindingCategory.PolicyBypass,
                Severity        = "CRITICAL",
                ConfidenceScore = 0.95f,
                Evidence        = "Hidden text detected (≥20 consecutive spaces or zero-width chars)",
                Explanation     = "Whitespace injection is used to hide instructions from humans but pass them to LLMs",
                Recommendation  = "Reject this file — it contains hidden text characters."
            });
        }

        // 2. White-on-white CSS
        if (WhiteTextCss.IsMatch(text))
        {
            var m = WhiteTextCss.Match(text);
            findings.Add(new Finding
            {
                RuleId          = "HI-001",
                RuleName        = DetectorName,
                Category        = FindingCategory.PolicyBypass,
                Severity        = "CRITICAL",
                ConfidenceScore = 0.90f,
                Evidence        = m.Value,
                LineNumber      = FindLineNumber(text, m.Value),
                Explanation     = "CSS white-on-white technique hides text from humans but LLMs can see it",
                Recommendation  = "Remove CSS hidden text styles from this file."
            });
        }

        // 3. Actionable HTML comments
        var htmlMatch = ActionableHtmlComment.Match(text);
        if (htmlMatch.Success)
        {
            findings.Add(new Finding
            {
                RuleId          = "HI-001",
                RuleName        = DetectorName,
                Category        = FindingCategory.PolicyBypass,
                Severity        = "CRITICAL",
                ConfidenceScore = 0.92f,
                Evidence        = htmlMatch.Value[..Math.Min(100, htmlMatch.Value.Length)],
                LineNumber      = FindLineNumber(text, htmlMatch.Value[..Math.Min(20, htmlMatch.Value.Length)]),
                Explanation     = "HTML comment contains actionable instruction — classic hidden injection",
                Recommendation  = "Remove all HTML comments with instructions."
            });
        }

        // 4. Hidden CSS markers (display:none etc.)
        foreach (var marker in HiddenMarkers)
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new Finding
                {
                    RuleId          = "HI-001",
                    RuleName        = DetectorName,
                    Category        = FindingCategory.PolicyBypass,
                    Severity        = "CRITICAL",
                    ConfidenceScore = 0.88f,
                    Evidence        = marker,
                    LineNumber      = FindLineNumber(text, marker),
                    Explanation     = $"CSS '{marker}' hides content from humans while LLMs still process it",
                    Recommendation  = "Remove hidden CSS styling."
                });
            }
        }

        // 5. Blank-line injection (≥10 blank lines before content)
        if (BlankLineInjection.IsMatch(text))
        {
            findings.Add(new Finding
            {
                RuleId          = "HI-001",
                RuleName        = DetectorName,
                Category        = FindingCategory.PolicyBypass,
                Severity        = "HIGH",
                ConfidenceScore = 0.80f,
                Evidence        = "10+ consecutive blank lines before instruction",
                Explanation     = "Whitespace padding pushes visible content off-screen in UI, hiding instructions from users",
                Recommendation  = "Remove excessive blank lines."
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
