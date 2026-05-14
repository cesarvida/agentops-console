using AgentOps.Core.Analysis;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Core.Analysis.Detectors;

/// <summary>
/// TA-001 — Detects tool abuse: instructions that tell an AI agent to
/// misuse built-in tools (file tool, terminal, code interpreter).
/// Supports: markdown and python.
/// </summary>
public class ToolAbuseRule : IPromptDetector
{
    public string DetectorName => "ToolAbuseRule";
    public string[] SupportedTypes => ["markdown", "python"];

    private static readonly (string pattern, string severity, string explanation)[] Rules =
    [
        // CRITICAL
        ("use the file tool to delete",      "CRITICAL", "Instructs agent to delete files via file tool"),
        ("call the terminal with rm",         "CRITICAL", "Instructs agent to run rm via terminal tool"),
        ("run this command in the terminal",  "CRITICAL", "Instructs agent to execute arbitrary terminal command"),
        ("use the bash tool to",             "CRITICAL", "Instructs agent to use bash tool (arbitrary execution)"),
        ("call the execute_code tool",        "CRITICAL", "Instructs agent to execute arbitrary code"),
        ("use the terminal tool to delete",   "CRITICAL", "Instructs agent to delete via terminal"),
        ("instruct the agent to run",         "CRITICAL", "Meta-instruction to execute arbitrary agent action"),

        // HIGH
        ("use your tools to access",          "HIGH", "Instructs agent to use tools to access files/data"),
        ("use code interpreter to run",       "HIGH", "Instructs agent to run code via code interpreter"),
        ("use the write_file tool",           "HIGH", "Instructs agent to write files"),
        ("call the web_search tool",          "HIGH", "Potentially harvesting data via search tool"),
        ("use the read_file tool on",         "HIGH", "Instructs agent to read potentially sensitive files"),
    ];

    public List<Finding> Analyze(ExtractedContent content, ContentContext context)
    {
        var findings = new List<Finding>();

        foreach (var (pattern, severity, explanation) in Rules)
        {
            var lineNum = FindLineNumber(content.RawText, pattern);
            if (lineNum < 0) continue;

            findings.Add(new Finding
            {
                RuleId          = "TA-001",
                RuleName        = DetectorName,
                Category        = FindingCategory.ToolAbuse,
                Severity        = severity,
                ConfidenceScore = 0.9f,
                Evidence        = ExtractLineEvidence(content.RawText, pattern),
                LineNumber      = lineNum,
                Explanation     = explanation,
                Recommendation  = "Remove tool-abuse instructions. Verify this file before passing to an agent."
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
