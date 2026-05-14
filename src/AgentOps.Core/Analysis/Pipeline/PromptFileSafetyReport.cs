using System.Text;
using System.Text.Json;

namespace AgentOps.Core.Analysis.Pipeline;

/// <summary>
/// Full safety analysis report for a prompt file (.md or .py).
/// Decision: PASS | REVIEW | BLOCK
/// </summary>
public class PromptFileSafetyReport
{
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>PASS / REVIEW / BLOCK</summary>
    public string Decision { get; set; } = string.Empty;

    public int RiskScore { get; set; }
    public string DecisionReason { get; set; } = string.Empty;
    public List<Finding> Findings { get; set; } = new();

    public int CriticalCount => Findings.Count(f => f.Severity == "CRITICAL");
    public int HighCount     => Findings.Count(f => f.Severity == "HIGH");
    public int MediumCount   => Findings.Count(f => f.Severity == "MEDIUM");
    public int LowCount      => Findings.Count(f => f.Severity == "LOW");

    public bool ObfuscationDetected { get; set; }
    public bool HiddenContentDetected { get; set; }
    public SanitizedPromptCandidate? SanitizedVersion { get; set; }

    // ── Scoring ───────────────────────────────────────────────────────────────
    /// <summary>
    /// CRITICAL=+30, HIGH=+20, MEDIUM=+10, LOW=+5, capped at 100.
    /// Extra +20 if obfuscation detected.
    /// </summary>
    public static int CalculateRiskScore(List<Finding> findings, bool obfuscated = false)
    {
        int score = 0;
        foreach (var f in findings)
        {
            score += f.Severity switch
            {
                "CRITICAL" => 30,
                "HIGH"     => 20,
                "MEDIUM"   => 10,
                "LOW"      => 5,
                _          => 0
            };
        }
        if (obfuscated) score += 20;
        return Math.Min(score, 100);
    }

    // ── Decision ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Determines PASS / REVIEW / BLOCK from findings, riskScore and obfuscation flag.
    /// </summary>
    public static (string decision, string reason) DetermineDecision(
        List<Finding> findings, int riskScore, bool obfuscated)
    {
        if (findings.Any(f => f.Severity == "CRITICAL"))
            return ("BLOCK", "Contains CRITICAL severity finding(s)");

        if (obfuscated)
            return ("BLOCK", "Obfuscation detected — possible hidden malicious content");

        if (riskScore > 60)
            return ("BLOCK", $"Risk score {riskScore} exceeds BLOCK threshold (60)");

        int highCount = findings.Count(f => f.Severity == "HIGH");
        if (highCount >= 2)
            return ("BLOCK", $"Multiple HIGH severity findings ({highCount})");

        int mediumCount = findings.Count(f => f.Severity == "MEDIUM");
        if (highCount >= 1 || mediumCount >= 3)
            return ("REVIEW", $"Requires human review — {highCount} HIGH, {mediumCount} MEDIUM finding(s)");

        if (riskScore is > 30 and <= 60)
            return ("REVIEW", $"Elevated risk score ({riskScore}) — manual review recommended");

        return ("PASS", "No significant threats detected");
    }

    // ── Markdown PR comment ───────────────────────────────────────────────────
    public string ToMarkdownComment()
    {
        var sb = new StringBuilder();
        var (emoji, badge) = Decision switch
        {
            "PASS"   => ("✅", "![PASS](https://img.shields.io/badge/AgentOps-PASS-brightgreen)"),
            "REVIEW" => ("⚠️", "![REVIEW](https://img.shields.io/badge/AgentOps-REVIEW-yellow)"),
            _        => ("🚫", "![BLOCK](https://img.shields.io/badge/AgentOps-BLOCK-red)")
        };

        sb.AppendLine($"## {emoji} AgentOps Prompt Security Analysis — `{FileName}`");
        sb.AppendLine();
        sb.AppendLine(badge);
        sb.AppendLine();
        sb.AppendLine($"| Field | Value |");
        sb.AppendLine($"|-------|-------|");
        sb.AppendLine($"| **Decision** | **{Decision}** |");
        sb.AppendLine($"| Risk Score | {RiskScore}/100 |");
        sb.AppendLine($"| File Type | {FileType} |");
        sb.AppendLine($"| Analyzed At | {AnalyzedAt:yyyy-MM-dd HH:mm:ss} UTC |");
        sb.AppendLine($"| Obfuscation | {(ObfuscationDetected ? "⚠️ YES" : "✅ No")} |");
        sb.AppendLine($"| Hidden Content | {(HiddenContentDetected ? "⚠️ YES" : "✅ No")} |");
        sb.AppendLine();

        if (Findings.Count > 0)
        {
            sb.AppendLine($"### Findings ({Findings.Count})");
            sb.AppendLine();
            sb.AppendLine("| Rule | Category | Severity | Confidence | Evidence |");
            sb.AppendLine("|------|----------|----------|------------|---------|");
            foreach (var f in Findings.OrderByDescending(x => x.Severity))
            {
                var sev = f.Severity switch
                {
                    "CRITICAL" => "🔴 CRITICAL",
                    "HIGH"     => "🟠 HIGH",
                    "MEDIUM"   => "🟡 MEDIUM",
                    _          => "🔵 LOW"
                };
                var evidence = f.Evidence.Length > 60
                    ? f.Evidence[..57] + "..."
                    : f.Evidence;
                sb.AppendLine($"| {f.RuleId} | {f.Category} | {sev} | {f.ConfidenceScore:P0} | `{evidence}` |");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("✅ No threats detected.");
            sb.AppendLine();
        }

        sb.AppendLine($"> **Reason:** {DecisionReason}");

        if (SanitizedVersion != null && Decision == "REVIEW")
        {
            sb.AppendLine();
            sb.AppendLine("### 🔧 Sanitized Version Available");
            sb.AppendLine($"> {SanitizedVersion.SanitizationNote}");
        }

        return sb.ToString();
    }

    // ── Audit JSON ────────────────────────────────────────────────────────────
    public string ToAuditJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}
