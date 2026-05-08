using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;

namespace AgentOps.Security.Rules
{
    public class MissingJustificationRule : ISecurityRule
    {
        public string Id => "CMP-RULE-004";
        public string Name => "Missing Justification for Processing";
        public string Description => "Detects when the agent definition lacks an explicit justification or rationale for processing data.";

        private static readonly string[] JustificationKeywords = new[] { "purpose", "justification", "justify", "rationale", "reason", "why" };

        public IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent)
        {
            var combined = (agent.Rules ?? new List<string>()).Concat(new[] { agent.Description ?? string.Empty, agent.Purpose ?? string.Empty });
            var text = string.Join(" ", combined).ToLowerInvariant();

            var found = JustificationKeywords.Any(k => text.Contains(k));
            if (!found)
            {
                yield return new SecurityFinding
                {
                    RuleId = Id,
                    RuleName = Name,
                    Severity = SecuritySeverity.Medium,
                    Location = "rules/description/purpose",
                    Summary = "Agent definition lacks explicit justification or rationale for data processing.",
                    EvidenceSummary = "No justification/rationale keywords found.",
                    Recommendation = "Include a clear justification/purpose for processing data to aid auditors."
                };
            }
        }
    }
}
