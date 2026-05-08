using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;

namespace AgentOps.Security.Rules
{
    public class NoComplianceRulesRule : ISecurityRule
    {
        public string Id => "CMP-RULE-000";
        public string Name => "No Compliance Rules Declared";
        public string Description => "Detects when an AgentDefinition contains no explicit compliance-related rules or keywords.";

        private static readonly string[] ComplianceKeywords = new[] { "retention", "consent", "lawful", "classification", "data minimization", "justification", "traceability", "retention policy", "privacy", "erasure", "right to erasure", "data retention" };

        public IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent)
        {
            var rules = agent.Rules ?? new List<string>();
            var combined = rules.Concat(new[] { agent.Description ?? string.Empty, agent.Purpose ?? string.Empty }).ToArray();
            var text = string.Join(" ", combined).ToLowerInvariant();

            var any = ComplianceKeywords.Any(k => text.Contains(k));
            if (!any)
            {
                yield return new SecurityFinding
                {
                    RuleId = Id,
                    RuleName = Name,
                    Severity = SecuritySeverity.Critical,
                    Location = "rules/description/purpose",
                    Summary = "No explicit compliance-related rules or keywords found in agent definition.",
                    EvidenceSummary = "Compliance checklist keywords not present.",
                    Recommendation = "Add explicit compliance rules (retention, lawful basis, classification, justification, traceability)."
                };
            }
        }
    }
}
