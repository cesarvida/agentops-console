using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Rule that requires agents to have audit logging configured.
    /// </summary>
    public class AuditLoggingRule : IGovernanceRule
    {
        private const int MinimumRetentionDays = 30;

        public string RuleName => "Audit Logging Required";
        public string Description => "Agent must have audit logging configured with minimum retention";
        public RuleSeverity Severity => RuleSeverity.Critical;

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

            if (agent?.Configuration?.RequiresAudit != true)
            {
                result.IsCompliant = false;
                result.Violations.Add("Audit logging is not configured (RequiresAudit must be true)");
                result.Recommendations.Add("Set RequiresAudit = true in agent configuration");
            }

            // Additional check: audit retention should be sufficient
            // (This would require extending AgentConfiguration if needed)
            
            if (!result.IsCompliant)
            {
                result.Recommendations.Add("Ensure audit.log_all_actions = true");
                result.Recommendations.Add($"Set audit.retention_days >= {MinimumRetentionDays}");
            }

            return Task.FromResult(result);
        }
    }
}
