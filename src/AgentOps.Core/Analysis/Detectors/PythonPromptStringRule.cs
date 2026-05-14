using System.Text.RegularExpressions;
using AgentOps.Core.Analysis;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Core.Analysis.Detectors;

/// <summary>
/// PS-001 — Detects dangerous Python patterns: code execution, environment dumps,
/// subprocess abuse, and mass filesystem access.
/// Supports: python only.
/// </summary>
public class PythonPromptStringRule : IPromptDetector
{
    public string DetectorName => "PythonPromptStringRule";
    public string[] SupportedTypes => ["python"];

    // eval(variable) — dynamic execution with external input
    private static readonly Regex EvalWithVar =
        new(@"\beval\s*\(\s*(?![\""'])[A-Za-z_]\w*", RegexOptions.Compiled);

    // exec(variable) — dynamic code execution
    private static readonly Regex ExecWithVar =
        new(@"\bexec\s*\(\s*(?![\""'])[A-Za-z_]\w*", RegexOptions.Compiled);

    // pickle.loads from external source
    private static readonly Regex PickleLoads =
        new(@"pickle\.loads\s*\(", RegexOptions.Compiled);

    // subprocess with shell=True
    private static readonly Regex SubprocessShell =
        new(@"subprocess\.\w+\s*\([^)]*shell\s*=\s*True", RegexOptions.Compiled | RegexOptions.Singleline);

    // os.system
    private static readonly Regex OsSystem =
        new(@"\bos\.system\s*\(", RegexOptions.Compiled);

    // os.environ dump
    private static readonly Regex OsEnvironDump =
        new(@"os\.environ(?:\.items\(\)|\.copy\(\)|\b)", RegexOptions.Compiled);

    // __import__ dynamic
    private static readonly Regex DynamicImport =
        new(@"__import__\s*\(", RegexOptions.Compiled);

    // compile() + exec()
    private static readonly Regex CompileExec =
        new(@"compile\s*\(.*\)\s*.*exec\s*\(", RegexOptions.Compiled | RegexOptions.Singleline);

    // os.walk() + open() — mass file access
    private static readonly Regex OsWalkOpen =
        new(@"os\.walk\s*\(.*open\s*\(", RegexOptions.Compiled | RegexOptions.Singleline);

    // glob("**/*") — mass discovery
    private static readonly Regex GlobMassDiscovery =
        new(@"glob\.glob\s*\(\s*[""']\*\*/?\*", RegexOptions.Compiled);

    public List<Finding> Analyze(ExtractedContent content, ContentContext context)
    {
        var findings = new List<Finding>();
        var text = content.RawText;
        var normalized = content.NormalizedText ?? content.RawText;

        // ── CRITICAL patterns ──────────────────────────────────────────────
        AddIfMatch(findings, EvalWithVar, text, normalized,
            "CRITICAL", 0.95f,
            "eval() with external variable — arbitrary code execution",
            "Never use eval() with user-controlled or external input.");

        AddIfMatch(findings, ExecWithVar, text, normalized,
            "CRITICAL", 0.95f,
            "exec() with external variable — arbitrary code execution",
            "Never use exec() with user-controlled input.");

        AddIfMatch(findings, PickleLoads, text, normalized,
            "CRITICAL", 0.92f,
            "pickle.loads() can execute arbitrary code during deserialization",
            "Use JSON or a safe serialization format instead of pickle.");

        AddIfMatch(findings, DynamicImport, text, normalized,
            "CRITICAL", 0.90f,
            "__import__() used for dynamic module loading — execution risk",
            "Replace with static imports.");

        AddIfMatch(findings, CompileExec, text, normalized,
            "CRITICAL", 0.92f,
            "compile() + exec() pattern — dynamic code execution",
            "Remove dynamic compilation.");

        // ── HIGH patterns ──────────────────────────────────────────────────
        AddIfMatch(findings, SubprocessShell, text, normalized,
            "HIGH", 0.88f,
            "subprocess with shell=True — command injection risk",
            "Use shell=False and pass arguments as a list.");

        AddIfMatch(findings, OsSystem, text, normalized,
            "HIGH", 0.85f,
            "os.system() — executes shell command directly",
            "Use subprocess with shell=False instead.");

        AddIfMatch(findings, OsEnvironDump, text, normalized,
            "HIGH", 0.82f,
            "os.environ access — may expose secrets and API keys",
            "Limit environment variable access; never expose all env vars.");

        // ── MEDIUM patterns ────────────────────────────────────────────────
        AddIfMatch(findings, OsWalkOpen, text, normalized,
            "MEDIUM", 0.70f,
            "os.walk() + open() — bulk file reading pattern",
            "Verify scope is limited to expected directories.");

        AddIfMatch(findings, GlobMassDiscovery, text, normalized,
            "MEDIUM", 0.68f,
            "glob(**/*) — mass filesystem discovery",
            "Limit glob patterns to specific directories.");

        // Context adjustment: test files get reduced confidence
        if (context.IsTestFile || context.IsDocumentation)
        {
            foreach (var f in findings)
                f.ConfidenceScore = Math.Max(0.2f, f.ConfidenceScore - 0.2f);
        }

        return findings;
    }

    private static void AddIfMatch(List<Finding> findings, Regex regex, string raw, string normalized,
        string severity, float confidence, string explanation, string recommendation)
    {
        var m = regex.Match(raw);
        bool fromNormalized = false;

        if (!m.Success && normalized != raw)
        {
            m = regex.Match(normalized);
            fromNormalized = m.Success;
        }

        if (!m.Success) return;

        findings.Add(new Finding
        {
            RuleId          = "PS-001",
            RuleName        = "PythonPromptStringRule",
            Category        = FindingCategory.PiiSecrets,
            Severity        = severity,
            ConfidenceScore = confidence,
            Evidence        = m.Value[..Math.Min(120, m.Value.Length)],
            LineNumber      = FindLineNumber(fromNormalized ? normalized : raw, m.Value),
            Explanation     = explanation,
            Recommendation  = recommendation,
            IsFromNormalized = fromNormalized
        });
    }

    private static int FindLineNumber(string text, string pattern)
    {
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains(pattern[..Math.Min(20, pattern.Length)], StringComparison.OrdinalIgnoreCase))
                return i + 1;
        }
        return -1;
    }
}
