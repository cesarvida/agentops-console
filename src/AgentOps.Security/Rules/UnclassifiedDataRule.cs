using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;

namespace AgentOps.Security.Rules
{
    public class UnclassifiedDataRule : ISecurityRule
    {
        public string Id => "CMP-RULE-003";
        public string Name => "Unclassified Data Handling";
        public string Description => "Detects when data classification controls or tools are not declared in the agent definition.";

        public IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent)
        {
            var hasClassificationTool = (agent.Tools ?? new List<string>()).Any(t => t.ToLowerInvariant().Contains("classification") || t.ToLowerInvariant().Contains("dataclassification") || t.ToLowerInvariant().Contains("data-classification"));
            var combined = (agent.Rules ?? new List<string>()).Concat(new[] { agent.Description ?? string.Empty }).Concat(new[] { agent.Purpose ?? string.Empty });
            var text = string.Join(" ", combined).ToLowerInvariant();
            var mentionsClassification = text.Contains("classification") || text.Contains("data classification") || text.Contains("classify");
            var negativeIndicators = text.Contains("without classification") || text.Contains("no classification") || text.Contains("not classified") || text.Contains("without classification controls") || text.Contains("no data classification");

            if (!hasClassificationTool && (negativeIndicators || !mentionsClassification))
            {
                yield return new SecurityFinding
                {
                    RuleId = Id,
                    RuleName = Name,
                    Severity = SecuritySeverity.High,
                    Location = "tools/rules/description",
                    Summary = "No data classification controls or tools declared; unclassified data handling risk.",
                    EvidenceSummary = "No 'classification' references in tools, rules or description.",
                    Recommendation = "Declare data classification controls or include a DataClassification tool in Tools."
                };
            }
        }
    }
}
