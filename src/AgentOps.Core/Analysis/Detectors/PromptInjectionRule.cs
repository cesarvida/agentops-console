using AgentOps.Core.Analysis;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Core.Analysis.Detectors;

/// <summary>
/// PI-001 — Detects prompt injection patterns.
/// Supports: markdown and python.
/// </summary>
public class PromptInjectionRule : IPromptDetector
{
    public string DetectorName => "PromptInjectionRule";
    public string[] SupportedTypes => ["markdown", "python"];

    private static readonly (string pattern, string severity, float confidence, string explanation)[] Rules =
    [
        // CRITICAL
        ("ignore previous instructions",    "CRITICAL", 0.95f, "Classic prompt injection: overrides system instructions"),
        ("disregard your instructions",     "CRITICAL", 0.95f, "Prompt injection: attempts to discard prior directives"),
        ("disregard all prior",             "CRITICAL", 0.95f, "Prompt injection: wipes prior context"),
        ("forget all previous instructions","CRITICAL", 0.95f, "Prompt injection: resets model instructions"),
        ("[SYSTEM]",                         "CRITICAL", 0.95f, "Fake system-level override attempt"),
        ("<system>",                         "CRITICAL", 0.95f, "Fake system tag injection"),
        ("your new instructions are",        "CRITICAL", 0.90f, "Prompt injection: replaces instructions mid-prompt"),
        ("override your previous",           "CRITICAL", 0.90f, "Prompt injection: explicit override attempt"),

        // HIGH
        ("act as if you have no restrictions", "HIGH", 0.80f, "Attempts to remove model safety restrictions"),
        ("do not reveal",                      "HIGH", 0.80f, "Attempting to hide prompt contents from users"),
        ("keep this secret",                   "HIGH", 0.80f, "Attempting to hide instructions"),
        ("don't tell the user",                "HIGH", 0.75f, "Hiding information from users"),
        ("pretend you are a",                  "HIGH", 0.75f, "Role-play injection to bypass guardrails"),
        ("act as a",                           "HIGH", 0.70f, "Role substitution injection"),
        ("you are now",                        "HIGH", 0.70f, "Identity override injection"),
    ];

    public List<Finding> Analyze(ExtractedContent content, ContentContext context)
    {
        var findings = new List<Finding>();
        var textSources = new[] { content.RawText, content.NormalizedText };

        foreach (var (pattern, severity, baseConf, explanation) in Rules)
        {
            foreach (var text in textSources.Distinct())
            {
                if (string.IsNullOrEmpty(text)) continue;

                var lineNum = FindLineNumber(text, pattern);
                if (lineNum < 0) continue;

                var evidence = ExtractLineEvidence(text, pattern);
                var conf = baseConf;

                // Reduce confidence for documentation/code examples
                if (context.IsDocumentation || context.IsCodeExample) conf -= 0.3f;
                if (context.IsTestFile) conf -= 0.2f;

                // If it's too low confidence, skip
                if (conf < 0.3f) continue;

                var isNormalized = text == content.NormalizedText && text != content.RawText;
                if (isNormalized && findings.Any(f => f.RuleId == "PI-001" && f.Evidence == evidence)) continue;

                findings.Add(new Finding
                {
                    RuleId          = "PI-001",
                    RuleName        = DetectorName,
                    Category        = FindingCategory.PromptInjection,
                    Severity        = severity,
                    ConfidenceScore = Math.Max(0f, conf),
                    Evidence        = evidence,
                    LineNumber      = lineNum,
                    Explanation     = explanation,
                    Recommendation  = "Remove or isolate this instruction pattern. Do not pass to LLM.",
                    IsFromNormalized = isNormalized
                });
                break; // Only report each pattern once (from first source that matches)
            }
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

    private static string ExtractLineEvidence(string text, string pattern)
    {
        foreach (var line in text.Split('\n'))
        {
            if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return line.Trim()[..Math.Min(120, line.Trim().Length)];
        }
        return pattern;
    }
}
