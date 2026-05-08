using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;

namespace AgentOps.Security.Rules
{
    public class MissingRetentionPolicyRule : ISecurityRule
    {
        public string Id => "CMP-RULE-001";
        public string Name => "Missing Retention Policy";
        public string Description => "Detects absence of data retention policy statements in the agent definition.";

        private static readonly string[] RetentionKeywords = new[] { "retention", "retention policy", "retention period", "delete after", "erasure", "erase", "destroy after", "retention_days" };

        public IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent)
        {
            var combined = (agent.Rules ?? new List<string>()).Concat(new[] { agent.Description ?? string.Empty, agent.Purpose ?? string.Empty });
            var text = string.Join(" ", combined).ToLowerInvariant();

            var found = RetentionKeywords.Any(k => text.Contains(k));
            if (!found)
            {
                yield return new SecurityFinding
                {
                    RuleId = Id,
                    RuleName = Name,
                    Severity = SecuritySeverity.High,
                    Location = "rules/description/purpose",
                    Summary = "No data retention policy or retention period specified in agent definition.",
                    EvidenceSummary = "No retention-related keyword found in rules, description or purpose.",
                    Recommendation = "Define a clear data retention policy (what to keep, for how long, and how to delete it)."
                };
            }
        }
    }
}
