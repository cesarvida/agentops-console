using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.Interfaces;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Governance;
using AgentOps.GitHub;

namespace AgentOps.Infrastructure.GitHub
{
    /// <summary>
    /// GitHub comment poster implementation using GitHub REST API.
    /// Includes retry logic with exponential backoff.
    /// </summary>
    public class GitHubCommentPoster : ICommentPoster
    {
        private readonly GitHubHttpClient _httpClient;
        private const int MaxRetries = 3;

        public GitHubCommentPoster(GitHubHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Posts an analysis comment to a GitHub PR if findings or low scores exist.
        /// Only posts if: HasFindings OR SecurityScore less than 70.
        /// Fails gracefully if API call fails.
        /// </summary>
        public async Task PostAnalysisCommentAsync(string owner, string repo, int prNumber, EvaluationReport report)
        {
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
                throw new ArgumentException("Owner and repo must be specified");

            if (report == null)
                throw new ArgumentNullException(nameof(report));

            // Only post if there are findings or security score is low
            bool shouldPost = (report.Findings?.Count ?? 0) > 0 || report.Metrics.SecurityScore < 70;

            if (!shouldPost)
                return;

            try
            {
                string comment = FormatAnalysisComment(report);
                _ = await PostCommentAsync(owner, repo, prNumber, comment);
            }
            catch (Exception ex)
            {
                // Log but don't throw - comment posting should not block the analysis flow
                Console.WriteLine($"[WARN] Failed to post analysis comment: {ex.Message}");
            }
        }

        /// <summary>
        /// Posts a governance validation report as a PR comment. Always posts regardless of score.
        /// Fails gracefully if API call fails.
        /// </summary>
        public async Task PostGovernanceReportAsync(string owner, string repo, int prNumber, GovernanceReport report)
        {
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
                throw new ArgumentException("Owner and repo must be specified");

            if (report == null)
                throw new ArgumentNullException(nameof(report));

            try
            {
                string comment = report.ToMarkdownComment();
                _ = await PostCommentAsync(owner, repo, prNumber, comment);
            }
            catch (Exception ex)
            {
                // Graceful fallback — comment failure must not block the workflow exit code
                Console.WriteLine($"[WARN] Failed to post governance report comment: {ex.Message}");
            }
        }

        /// <summary>
        /// Formats the evaluation report as a GitHub PR comment in markdown.
        /// </summary>
        private string FormatAnalysisComment(EvaluationReport report)
        {
            var lines = new List<string>();

            // Header
            lines.Add("## AgentOps Security Analysis");
            lines.Add("");

            // Status line
            string statusEmoji = report.FinalStatus switch
            {
                "PASS" => "PASS",
                "REVIEW" => "REVIEW",
                "FAIL" => "FAIL",
                _ => "UNKNOWN"
            };
            lines.Add($"**Status:** {report.FinalStatus} [{statusEmoji}]");
            lines.Add($"**Security Score:** {report.Metrics.SecurityScore}/100");
            lines.Add($"**Risk Level:** {report.OverallRiskLevel}");
            lines.Add("");

            // Findings section
            int findingsCount = report.Findings?.Count ?? 0;
            lines.Add($"### Findings ({findingsCount})");

            if (findingsCount > 0)
            {
                lines.Add("");
                lines.Add("| Pattern | Severity | Summary |");
                lines.Add("|---------|----------|---------|");

                foreach (var finding in report.Findings)
                {
                    string severity = finding.Severity ?? "Unknown";
                    string summary = RedactSensitiveData(finding.Summary ?? "");
                    lines.Add($"| {finding.Category} | {severity} | {summary} |");
                }
            }
            else
            {
                lines.Add("");
                lines.Add("No security findings detected.");
            }

            lines.Add("");

            // Metrics section
            lines.Add("### Evaluation Metrics");
            lines.Add("");
            lines.Add($"- **Compliance Score:** {report.Metrics.ComplianceScore}/100");
            lines.Add($"- **Consistency Score:** {report.Metrics.ConsistencyScore}/100");
            lines.Add($"- **Explainability Score:** {report.Metrics.ExplainabilityScore}/100");
            lines.Add($"- **Combined Quality:** {report.Metrics.CombinedQualityScore}/100");
            lines.Add("");

            // Footer
            lines.Add("---");
            lines.Add("*Generated by AgentOps Console*");

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Redacts sensitive data from finding summaries.
        /// </summary>
        private string RedactSensitiveData(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Redact common secret patterns
            text = System.Text.RegularExpressions.Regex.Replace(
                text, 
                @"(sk-[a-zA-Z0-9]{20,}|pk-[a-zA-Z0-9]{20,}|ghp_[a-zA-Z0-9]{36})", 
                "[REDACTED]"
            );

            // Redact API keys and tokens
            text = System.Text.RegularExpressions.Regex.Replace(
                text,
                @"(api[_-]?key|token|password)[\s]*=[\s]*['""a-zA-Z0-9]+",
                "$1=[REDACTED]",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );

            return text;
        }

        /// <summary>
        /// Posts a comment to a GitHub PR using the GitHub API with retry logic.
        /// Attempts up to 3 times with exponential backoff (2s, 4s).
        /// If all retries fail, writes a GitHub Actions warning instead of throwing.
        /// </summary>
        private async Task<bool> PostCommentAsync(string owner, string repo, int prNumber, string commentBody)
        {
            var endpoint = $"/repos/{owner}/{repo}/issues/{prNumber}/comments";
            var payload = new { body = commentBody };
            var jsonContent = JsonSerializer.Serialize(payload);

            return await PostWithRetryAsync(owner, repo, prNumber, endpoint, jsonContent);
        }

        /// <summary>
        /// Attempts to post a comment with exponential backoff retry (max 3 attempts).
        /// Returns true if successful, false if all retries exhausted.
        /// Writes GitHub Actions warning if all attempts fail.
        /// </summary>
        private async Task<bool> PostWithRetryAsync(string owner, string repo, int prNumber, string endpoint, string jsonContent)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    await _httpClient.PostAsync(endpoint, jsonContent);
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[INFO] Attempt {attempt}/{MaxRetries} to post PR comment failed: {ex.Message}");

                    if (attempt < MaxRetries)
                    {
                        // Exponential backoff: 2s, 4s
                        var delayMs = attempt * 2000;
                        await Task.Delay(delayMs);
                    }
                }
            }

            // All retries exhausted — write GitHub Actions warning
            Console.WriteLine("::warning::AgentOps: PR comment could not be posted after 3 attempts. Governance report is in the workflow artifacts.");
            return false;
        }
    }
}
