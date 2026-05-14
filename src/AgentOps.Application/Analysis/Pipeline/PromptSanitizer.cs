using System.Text.RegularExpressions;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Application.Analysis.Pipeline;

/// <summary>
/// Generates a sanitized version of a prompt file for REVIEW-level decisions.
/// Removes or redacts suspicious sections while preserving the rest.
/// </summary>
public class PromptSanitizer
{
    private static readonly Regex HtmlComment =
        new(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex MarkdownCodeBlock =
        new(@"```[\w]*\r?\n.*?```", RegexOptions.Singleline | RegexOptions.Compiled);

    public SanitizedPromptCandidate Sanitize(string content, List<Finding> findings)
    {
        var candidate = new SanitizedPromptCandidate
        {
            OriginalContent = content
        };

        var sanitized = content;

        // Remove HTML comments
        foreach (Match m in HtmlComment.Matches(sanitized))
        {
            candidate.RemovedSections.Add($"HTML comment: {m.Value[..Math.Min(50, m.Value.Length)]}...");
        }
        sanitized = HtmlComment.Replace(sanitized, "<!-- [REDACTED by AgentOps] -->");

        // Remove suspicious code blocks that contain dangerous patterns
        foreach (var finding in findings.Where(f => f.Severity is "HIGH" or "CRITICAL"))
        {
            if (!string.IsNullOrEmpty(finding.Evidence))
            {
                var escaped = Regex.Escape(finding.Evidence[..Math.Min(40, finding.Evidence.Length)]);
                sanitized = Regex.Replace(sanitized, escaped + @"[^\n]*", "[REDACTED by AgentOps]",
                    RegexOptions.IgnoreCase);
                candidate.RemovedSections.Add($"[{finding.Severity}] {finding.RuleId}: {finding.Evidence[..Math.Min(50, finding.Evidence.Length)]}");
            }
        }

        candidate.SanitizedContent = sanitized;
        candidate.IsSafeToUse = !findings.Any(f => f.Severity == "CRITICAL");
        candidate.SanitizationNote = $"Removed {candidate.RemovedSections.Count} suspicious section(s). " +
            (candidate.IsSafeToUse ? "Safe to use after review." : "Still contains high-risk content.");

        return candidate;
    }
}
