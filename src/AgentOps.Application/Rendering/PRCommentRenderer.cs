using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;

namespace AgentOps.Application.Rendering
{
    /// <summary>
    /// Renders an EvaluationReport as a GitHub PR comment in Markdown format.
    /// Follows GitHub Flavored Markdown and includes a marker comment for deduplication.
    /// </summary>
    public static class PRCommentRenderer
    {
        private const string CommentMarker = "<!-- agentops-pr-analysis -->";

        /// <summary>
        /// Renders a complete PR comment with HTML marker for deduplication.
        /// </summary>
        public static string RenderComment(EvaluationReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            var sb = new StringBuilder();
            sb.AppendLine(CommentMarker);
            sb.AppendLine();
            sb.AppendLine(RenderHeader(report));
            sb.AppendLine();
            sb.AppendLine(RenderSummary(report));
            sb.AppendLine();

            if (report.Findings.Any())
            {
                sb.AppendLine(RenderFindings(report));
                sb.AppendLine();
            }

            sb.AppendLine(RenderMetrics(report));
            sb.AppendLine();
            sb.AppendLine(RenderRecommendations(report));
            sb.AppendLine();
            sb.AppendLine(RenderFooter());

            return sb.ToString();
        }

        /// <summary>
        /// Returns the deduplication marker to search for existing comments.
        /// </summary>
        public static string GetCommentMarker() => CommentMarker;

        private static string RenderHeader(EvaluationReport report)
        {
            var emoji = report.FinalStatus switch
            {
                "PASS" => "✅",
                "REVIEW" => "⚠️",
                "FAIL" or "BLOCK" => "❌",
                _ => "🔍"
            };

            return $"## {emoji} AgentOps PR Analysis — {report.FinalStatus}";
        }

        private static string RenderSummary(EvaluationReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("**Status Overview:**");
            sb.AppendLine();
            sb.AppendLine($"| Property | Value |");
            sb.AppendLine("|----------|-------|");
            sb.AppendLine($"| Final Status | **{report.FinalStatus}** |");
            sb.AppendLine($"| Risk Level | {GetRiskEmoji(report.OverallRiskLevel)} {report.OverallRiskLevel} |");
            sb.AppendLine($"| Combined Quality Score | {report.Metrics?.CombinedQualityScore ?? 0}/100 |");
            sb.AppendLine($"| Total Findings | {report.Findings.Count} |");
            sb.AppendLine($"| Critical Findings | {report.Metrics?.CriticalFindingsCount ?? 0} |");

            return sb.ToString();
        }

        private static string RenderFindings(EvaluationReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("### 🔍 Findings (Top 5)");
            sb.AppendLine();

            var topFindings = report.Findings
                .OrderByDescending(f => SeverityToInt(f.Severity))
                .Take(5)
                .ToList();

            if (!topFindings.Any())
            {
                sb.AppendLine("No findings detected.");
                return sb.ToString();
            }

            foreach (var finding in topFindings)
            {
                var severityEmoji = finding.Severity switch
                {
                    "Critical" => "🔴",
                    "High" => "🟠",
                    "Medium" => "🟡",
                    "Low" => "🟢",
                    _ => "⚪"
                };

                sb.AppendLine($"- **{severityEmoji} {finding.Severity}** | {finding.Category}");
                sb.AppendLine($"  - {finding.Summary}");
                
                if (!string.IsNullOrEmpty(finding.Location))
                    sb.AppendLine($"  - Location: `{finding.Location}`");

                if (finding.Confidence.HasValue)
                    sb.AppendLine($"  - Confidence: {(finding.Confidence.Value * 100):F0}%");
            }

            if (report.Findings.Count > 5)
                sb.AppendLine($"\\n*... and {report.Findings.Count - 5} more findings. See full report for details.*");

            return sb.ToString();
        }

        private static string RenderMetrics(EvaluationReport report)
        {
            var metrics = report.Metrics ?? new Metrics();
            
            var sb = new StringBuilder();
            sb.AppendLine("### 📊 Quality Metrics");
            sb.AppendLine();
            sb.AppendLine($"| Metric | Score |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| Security | {GetScoreBar(metrics.SecurityScore)} {metrics.SecurityScore} |");
            sb.AppendLine($"| Compliance | {GetScoreBar(metrics.ComplianceScore)} {metrics.ComplianceScore} |");
            sb.AppendLine($"| Consistency | {GetScoreBar(metrics.ConsistencyScore)} {metrics.ConsistencyScore} |");
            sb.AppendLine($"| Explainability | {GetScoreBar(metrics.ExplainabilityScore)} {metrics.ExplainabilityScore} |");

            return sb.ToString();
        }

        private static string RenderRecommendations(EvaluationReport report)
        {
            if (!report.Recommendations.Any())
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine("### 💡 Recommendations");
            sb.AppendLine();

            foreach (var rec in report.Recommendations.Take(3))
            {
                sb.AppendLine($"**{rec.Title}** ({rec.SeverityImpact} Impact)");
                sb.AppendLine($"  {rec.Description}");
                
                if (!string.IsNullOrEmpty(rec.EffortEstimate))
                    sb.AppendLine($"  Effort: {rec.EffortEstimate}");
                
                sb.AppendLine();
            }

            if (report.Recommendations.Count > 3)
                sb.AppendLine($"*... and {report.Recommendations.Count - 3} more recommendations.*");

            return sb.ToString();
        }

        private static string RenderFooter()
        {
            var timestamp = DateTime.UtcNow.ToString("O");
            return $"---\\n_Generated by AgentOps on {timestamp}_\\n_For details, check the build artifacts._";
        }

        private static string GetRiskEmoji(string? riskLevel) => riskLevel?.ToLowerInvariant() switch
        {
            "low" => "🟢",
            "medium" => "🟡",
            "high" => "🔴",
            "critical" => "⛔",
            _ => "⚪"
        };

        private static string GetScoreBar(int score)
        {
            var filled = (score / 10);
            var empty = 10 - filled;
            return $"`{'█'.ToString().PadRight(filled, '█').PadRight(10, '░')}`";
        }

        private static int SeverityToInt(string severity) => severity?.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };
    }
}
