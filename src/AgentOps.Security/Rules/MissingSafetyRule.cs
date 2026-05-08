using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;

namespace AgentOps.Security.Rules
{
    public class MissingSafetyRule : ISecurityRule
    {
        public string Id => "SEC-RULE-004";
        public string Name => "Missing Safety Requirements";
        public string Description => "Detects when agent definitions lack minimal stated safety/privacy constraints.";

        private static readonly string[] SafetyKeywords = new[] { "do not disclose", "do not share", "respect privacy", "data minimization", "follow policy", "do not reveal" };

        public IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent)
        {
            var combined = (agent.Rules ?? new System.Collections.Generic.List<string>()).Concat(new[] { agent.Description ?? string.Empty, agent.Purpose ?? string.Empty });
            var text = string.Join(" ", combined).ToLowerInvariant();

            var found = SafetyKeywords.Any(k => text.Contains(k));
            if (!found)
            {
                yield return new SecurityFinding
                {
                    RuleId = Id,
                    RuleName = Name,
                    Severity = SecuritySeverity.Medium,
                    Location = "rules/description/purpose",
                    Summary = "AgentDefinition does not state minimal safety or privacy constraints.",
                    EvidenceSummary = "No safety keyword present.",
                    Evidence = null,
                    Recommendation = "Add explicit safety and privacy constraints to agent rules."
                };
            }
        }
    }
}
