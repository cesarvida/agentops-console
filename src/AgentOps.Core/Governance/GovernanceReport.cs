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
        /// True when at least one Critical violation was downgraded to Warning
        /// because of an active <see cref="GovernanceException"/>.
        /// </summary>
        public bool HasExceptionOverrides
            => RuleResults.Any(r => !string.IsNullOrEmpty(r.ExceptionNote));

        /// <summary>
        /// Converts the governance report to a markdown comment suitable for posting on GitHub PRs.
        /// </summary>
        /// <returns>A markdown-formatted string suitable for PR comments.</returns>
        public string ToMarkdownComment()
        {
            var sb = new StringBuilder();

            string statusEmoji = FinalStatus switch
            {
                "APPROVED" => "✅",
                "REVIEW" => "⚠️",
                "BLOCKED" => "❌",
                _ => "❓"
            };

            // Header
            sb.AppendLine("## 🛡️ AgentOps Governance Report");
            sb.AppendLine();
            sb.AppendLine($"**Agente:** {AgentName} v{AgentVersion}  ");
            sb.AppendLine($"**Estado:** {statusEmoji} {FinalStatus}  ");
            sb.AppendLine($"**Governance Score:** {GovernanceScore}/100  ");
            sb.AppendLine();

            // Rules table
            sb.AppendLine("### Reglas evaluadas");
            sb.AppendLine();
            sb.AppendLine("| Regla | Severidad | Estado |");
            sb.AppendLine("|-------|-----------|--------|");

            foreach (var rule in RuleResults)
            {
                string severityIcon = rule.Severity switch
                {
                    RuleSeverity.Critical => "🔴 Critical",
                    RuleSeverity.Warning  => "🟡 Warning",
                    _                     => "ℹ️ Info"
                };
                string stateIcon = rule.IsCompliant
                    ? (rule.Severity == RuleSeverity.Warning ? "✅ Pass" : "✅ Pass")
                    : (rule.Severity == RuleSeverity.Warning ? "⚠️ Warn" : "❌ Fail");

                sb.AppendLine($"| {rule.RuleName} | {severityIcon} | {stateIcon} |");
            }

            sb.AppendLine();

            // Violations section (only when there are failures)
            var failedRules = RuleResults.Where(r => !r.IsCompliant).ToList();
            if (failedRules.Any())
            {
                sb.AppendLine("### ❌ Violaciones encontradas");
                sb.AppendLine();
                foreach (var rule in failedRules)
                {
                    foreach (var violation in rule.Violations)
                        sb.AppendLine($"- **{rule.RuleName}**: {violation}");
                    foreach (var rec in rule.Recommendations)
                        sb.AppendLine($"  - 💡 {rec}");
                }
                sb.AppendLine();
            }

            // Verdict footer
            if (FinalStatus == "BLOCKED")
            {
                sb.AppendLine("> ⛔ Este agente tiene violaciones críticas y NO puede desplegarse.");
                sb.AppendLine("> Corrige las violaciones antes de mergear este PR.");
            }
            else if (FinalStatus == "REVIEW")
            {
                sb.AppendLine("> ⚠️ Este agente necesita revisión humana antes de desplegarse.");
            }
            else if (FinalStatus == "APPROVED")
            {
                sb.AppendLine("> ✅ Este agente cumple todas las reglas de governance.");
            }

            sb.AppendLine();
            sb.AppendLine("*Generado por AgentOps Governance Engine*");

            return sb.ToString();
        }
    }
}
