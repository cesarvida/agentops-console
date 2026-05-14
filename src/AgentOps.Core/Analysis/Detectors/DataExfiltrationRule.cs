using System.Text.RegularExpressions;
using AgentOps.Core.Analysis;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Core.Analysis.Detectors;

/// <summary>
/// DE-001 — Detects data exfiltration patterns.
/// Supports: markdown and python.
/// </summary>
public class DataExfiltrationRule : IPromptDetector
{
    public string DetectorName => "DataExfiltrationRule";
    public string[] SupportedTypes => ["markdown", "python"];

    // Python: requests.post(...) near file content
    private static readonly Regex RequestsPostWithFile =
        new(@"requests\.post\s*\([^)]*(?:open|read|content)[^)]*\)", RegexOptions.Compiled | RegexOptions.Singleline);

    // open() and requests.post in same scope (within 5 lines of each other)
    private static readonly Regex OpenAndRequests =
        new(@"open\s*\([^)]+\).*requests\.(post|get|put)", RegexOptions.Compiled | RegexOptions.Singleline);

    // base64.encode + requests
    private static readonly Regex Base64Requests =
        new(@"base64\.(b64encode|encodebytes).*requests\.(post|put)", RegexOptions.Compiled | RegexOptions.Singleline);

    // URL pattern
    private static readonly Regex UrlPattern =
        new(@"https?://[^\s\)\]>""']+", RegexOptions.Compiled);

    // Known exfiltration hosts
    private static readonly string[] ExfiltrationHosts =
        ["webhook.site", "requestbin", "ngrok.io", "pipedream.net",
         "hookbin.com", "beeceptor.com", "canarytokens", "interact.sh"];

    private static readonly (string pattern, string severity, string explanation)[] TextPatterns =
    [
        // CRITICAL: direct exfil instructions
        ("send the contents of",   "CRITICAL", "Instructs sending file contents to external destination"),
        ("send all the files",     "CRITICAL", "Bulk file exfiltration instruction"),
        ("exfiltrate",             "CRITICAL", "Explicit exfiltration keyword"),
        ("upload the system files","CRITICAL", "Instructs uploading system files"),
        ("transmit the data",      "CRITICAL", "Data transmission instruction"),

        // HIGH
        ("upload the file to",    "HIGH", "File upload to external destination"),
        ("forward this to",       "HIGH", "Data forwarding instruction"),
        ("send this email to",    "HIGH", "Data forwarding via email"),

        // Python modules for exfiltration
        ("ftplib",                "HIGH", "FTP library — common exfiltration vector"),
        ("paramiko",              "HIGH", "SSH/SFTP library — potential exfiltration vector"),
        ("socket.connect",        "HIGH", "Raw socket connection — potential exfiltration"),
    ];

    public List<Finding> Analyze(ExtractedContent content, ContentContext context)
    {
        var findings = new List<Finding>();

        // --- Text pattern matching on raw + normalized
        var textSources = new[] { content.RawText, content.NormalizedText };
        foreach (var (pattern, severity, explanation) in TextPatterns)
        {
            foreach (var text in textSources.Distinct())
            {
                if (string.IsNullOrEmpty(text)) continue;
                var lineNum = FindLineNumber(text, pattern);
                if (lineNum < 0) continue;

                var evidence = ExtractLineEvidence(text, pattern);
                bool isNormalized = text == content.NormalizedText && text != content.RawText;

                findings.Add(new Finding
                {
                    RuleId          = "DE-001",
                    RuleName        = DetectorName,
                    Category        = FindingCategory.DataExfiltration,
                    Severity        = severity,
                    ConfidenceScore = 0.90f,
                    Evidence        = evidence,
                    LineNumber      = lineNum,
                    Explanation     = explanation,
                    Recommendation  = "Remove exfiltration instructions. Never send file contents to external URLs.",
                    IsFromNormalized = isNormalized
                });
                break;
            }
        }

        // --- Python-specific regex patterns
        if (content.FileType == "python")
        {
            CheckRegex(findings, content.RawText, RequestsPostWithFile,
                "CRITICAL", "requests.post() with file content — data exfiltration pattern");

            CheckRegex(findings, content.RawText, OpenAndRequests,
                "CRITICAL", "open() + requests.post in same scope — classic exfiltration");

            CheckRegex(findings, content.RawText, Base64Requests,
                "CRITICAL", "base64 encode + requests.post — obfuscated exfiltration");
        }

        // --- Check URLs against known exfiltration hosts
        foreach (var url in content.Urls)
        {
            var host = ExtractHost(url);
            if (ExfiltrationHosts.Any(h => host.Contains(h, StringComparison.OrdinalIgnoreCase)))
            {
                findings.Add(new Finding
                {
                    RuleId          = "DE-001",
                    RuleName        = DetectorName,
                    Category        = FindingCategory.DataExfiltration,
                    Severity        = "CRITICAL",
                    ConfidenceScore = 0.95f,
                    Evidence        = url,
                    Explanation     = $"Known exfiltration host detected: {host}",
                    Recommendation  = "Remove this URL immediately — it points to a known data-collection endpoint."
                });
            }
        }

        // --- URLs in markdown instructions (MEDIUM, context-dependent)
        if (content.FileType == "markdown" && content.Urls.Count > 0)
        {
            var instructionText = content.RawText;
            foreach (var url in content.Urls)
            {
                var lineNum = FindLineNumber(instructionText, url);
                if (lineNum >= 0 && HasActionableContext(instructionText, url))
                {
                    findings.Add(new Finding
                    {
                        RuleId          = "DE-001",
                        RuleName        = DetectorName,
                        Category        = FindingCategory.DataExfiltration,
                        Severity        = "MEDIUM",
                        ConfidenceScore = 0.60f,
                        Evidence        = url,
                        LineNumber      = lineNum,
                        Explanation     = "URL found in actionable instruction context",
                        Recommendation  = "Verify this URL is not used to collect or transmit user data."
                    });
                }
            }
        }

        return findings;
    }

    private static void CheckRegex(List<Finding> findings, string text, Regex re,
        string severity, string explanation)
    {
        var m = re.Match(text);
        if (!m.Success) return;
        findings.Add(new Finding
        {
            RuleId          = "DE-001",
            RuleName        = "DataExfiltrationRule",
            Category        = FindingCategory.DataExfiltration,
            Severity        = severity,
            ConfidenceScore = 0.92f,
            Evidence        = m.Value[..Math.Min(120, m.Value.Length)],
            Explanation     = explanation,
            Recommendation  = "Remove data exfiltration code."
        });
    }

    private static bool HasActionableContext(string text, string url)
    {
        int idx = text.IndexOf(url, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return false;
        var context = text[Math.Max(0, idx - 100)..Math.Min(text.Length, idx + 100)].ToLowerInvariant();
        return context.Contains("send") || context.Contains("post") || context.Contains("upload") ||
               context.Contains("forward") || context.Contains("submit");
    }

    private static string ExtractHost(string url)
    {
        try { return new Uri(url).Host; } catch { return url; }
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
