using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AgentOps.Core.Governance
{
    /// <summary>
    /// Comprehensive governance evaluation report for an agent definition.
    /// </summary>
    public class GovernanceReport
    {
        /// <summary>Gets or sets the agent ID being evaluated.</summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>Gets or sets the agent name being evaluated.</summary>
        public string AgentName { get; set; } = string.Empty;

        /// <summary>Gets or sets the agent version being evaluated.</summary>
        public string AgentVersion { get; set; } = string.Empty;

        /// <summary>Gets or sets whether the agent is compliant (no critical violations).</summary>
        public bool IsCompliant { get; set; }

        /// <summary>Gets or sets the governance score (0-100).</summary>
        public int GovernanceScore { get; set; }

        /// <summary>Gets or sets the final status: APPROVED, REVIEW, or BLOCKED.</summary>
        public string FinalStatus { get; set; } = "REVIEW";

        /// <summary>Gets or sets the detailed results from each rule evaluation.</summary>
        public List<RuleResult> RuleResults { get; set; } = new();

        /// <summary>Gets or sets the count of critical violations.</summary>
        public int CriticalViolations { get; set; }

        /// <summary>Gets or sets the count of warning violations.</summary>
        public int WarningViolations { get; set; }

        /// <summary>Gets or sets the timestamp when this report was generated.</summary>
        public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Converts the governance report to a markdown comment suitable for posting on GitHub PRs.
        /// </summary>
        /// <returns>A markdown-formatted string suitable for PR comments.</returns>
        public string ToMarkdownComment()
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("## 🛡️ AgentOps Governance Report");
            sb.AppendLine();

            // Status line with emoji
            string statusEmoji = FinalStatus switch
            {
                "APPROVED" => "✅",
                "REVIEW" => "⚠️",
                "BLOCKED" => "❌",
                _ => "❓"
            };

            sb.AppendLine($"**Status:** {FinalStatus} {statusEmoji}");
            sb.AppendLine($"**Agent:** {AgentName} v{AgentVersion}");
            sb.AppendLine($"**Governance Score:** {GovernanceScore}/100");
            sb.AppendLine();

            // Violations summary
            sb.AppendLine("### Validation Results");
            sb.AppendLine();
            sb.AppendLine($"- **Rules Passed:** {RuleResults.Count(r => r.IsCompliant)}/{RuleResults.Count}");
            sb.AppendLine($"- **Critical Violations:** {CriticalViolations}");
            sb.AppendLine($"- **Warnings:** {WarningViolations}");
            sb.AppendLine();

            // Details by rule
            if (RuleResults.Any(r => !r.IsCompliant))
            {
                sb.AppendLine("### Rule Violations");
                sb.AppendLine();

                foreach (var rule in RuleResults.Where(r => !r.IsCompliant))
                {
                    string icon = rule.Severity switch
                    {
                        RuleSeverity.Critical => "🔴",
                        RuleSeverity.Warning => "🟡",
                        RuleSeverity.Info => "ℹ️",
                        _ => "❓"
                    };

                    sb.AppendLine($"{icon} **{rule.RuleName}** ({rule.Severity})");

                    if (rule.Violations.Any())
                    {
                        foreach (var violation in rule.Violations)
                        {
                            sb.AppendLine($"  - {violation}");
                        }
                    }

                    if (rule.Recommendations.Any())
                    {
                        sb.AppendLine($"  **Recommendations:**");
                        foreach (var recommendation in rule.Recommendations)
                        {
                            sb.AppendLine($"  - {recommendation}");
                        }
                    }

                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("### Validation Status");
                sb.AppendLine("✅ Agent passed all governance rules!");
                sb.AppendLine();
            }

            // Footer with verdict
            if (FinalStatus == "BLOCKED")
            {
                sb.AppendLine("> ⛔ This PR is **BLOCKED**. Resolve all critical violations before resubmitting.");
            }
            else if (FinalStatus == "REVIEW")
            {
                sb.AppendLine("> ⚠️ This PR requires **manual review**. Address the warnings above.");
            }
            else if (FinalStatus == "APPROVED")
            {
                sb.AppendLine("> ✅ This agent complies with governance rules and may be deployed.");
            }

            sb.AppendLine();
            sb.AppendLine("*Generated by AgentOps Governance Engine*");

            return sb.ToString();
        }
    }
}
