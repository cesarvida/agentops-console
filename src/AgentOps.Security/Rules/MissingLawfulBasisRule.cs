using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;

namespace AgentOps.Security.Rules
{
    public class MissingLawfulBasisRule : ISecurityRule
    {
        public string Id => "CMP-RULE-002";
        public string Name => "Missing Lawful Basis for Processing";
        public string Description => "Detects when the definition mentions personal data processing without an explicit lawful basis (consent, contract, legitimate interest).";

        private static readonly string[] PiiIndicators = new[] { "personal data", "pii", "personal identifiable", "personal information", "ssn", "social security", "email address", "phone number" };
        private static readonly string[] LawfulKeywords = new[] { "consent", "lawful basis", "legitimate interest", "contract", "legal basis", "consent obtained", "consent required" };

        public IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent)
        {
            var combined = (agent.Rules ?? new List<string>()).Concat(new[] { agent.Description ?? string.Empty, agent.Purpose ?? string.Empty });
            var text = string.Join(" ", combined).ToLowerInvariant();

            var mentionsPii = PiiIndicators.Any(k => text.Contains(k));
            var hasLawful = LawfulKeywords.Any(k => text.Contains(k));

            if (mentionsPii && !hasLawful)
            {
                yield return new SecurityFinding
                {
                    RuleId = Id,
                    RuleName = Name,
                    Severity = SecuritySeverity.Critical,
                    Location = "rules/description/purpose",
                    Summary = "Agent references processing or storage of personal data without stating a lawful basis.",
                    EvidenceSummary = "PII indicators found but no lawful basis keywords present.",
                    Recommendation = "State the lawful basis (e.g., consent, contract) and document how consent is obtained or relied upon."
                };
            }
        }
    }
}
