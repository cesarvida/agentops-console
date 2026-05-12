using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Rule that validates agents only declare allowed actions from a whitelist.
    /// </summary>
    public class AllowedActionsRule : IGovernanceRule
    {
        private static readonly HashSet<string> AllowedActions = new()
        {
            "read_code",
            "post_comment",
            "request_changes",
            "read_files",
            "read_logs",
            "send_notification",
            "create_report"
        };

        public string RuleName => "Allowed Actions Whitelist";
        public string Description => "Agent can only declare actions from the allowed whitelist";
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

            if (agent?.Configuration?.AllowedActions == null || !agent.Configuration.AllowedActions.Any())
            {
                result.IsCompliant = true; // No actions declared is OK
                return Task.FromResult(result);
            }

            var invalidActions = agent.Configuration.AllowedActions
                .Where(action => !AllowedActions.Contains(action))
                .ToList();

            if (invalidActions.Any())
            {
                result.IsCompliant = false;
                foreach (var action in invalidActions)
                {
                    result.Violations.Add($"Action '{action}' is not in the allowed whitelist");
                }
                result.Recommendations.Add($"Remove or replace with one of these actions: {string.Join(", ", AllowedActions)}");
            }

            return Task.FromResult(result);
        }
    }
}
