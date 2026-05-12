using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Rule that recommends agents have an owner defined.
    /// Warning-level: doesn't block but lowers the score.
    /// </summary>
    public class OwnerDefinedRule : IGovernanceRule
    {
        public string RuleName => "Owner Defined";
        public string Description => "Agent should have a responsible owner assigned";
        public RuleSeverity Severity => RuleSeverity.Warning;

        public Task<RuleResult> EvaluateAsync(AgentDefinition agent)
        {
            var result = new RuleResult
            {
                RuleName = RuleName,
                Severity = Severity,
                IsCompliant = true,
                Violations = new(),
                Recommendations = new()
            };

            if (string.IsNullOrWhiteSpace(agent?.Configuration?.Owner))
            {
                result.IsCompliant = false;
                result.Violations.Add("No owner is defined for this agent");
                result.Recommendations.Add("Add an owner field in the agent definition YAML");
                result.Recommendations.Add("Example: owner: 'team-backend' or owner: 'alice@company.com'");
            }

            return Task.FromResult(result);
        }
    }
}
