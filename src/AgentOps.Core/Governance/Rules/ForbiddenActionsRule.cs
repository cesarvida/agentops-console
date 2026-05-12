using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Rule that blocks agents from declaring forbidden actions.
    /// </summary>
    public class ForbiddenActionsRule : IGovernanceRule
    {
        private static readonly HashSet<string> ForbiddenActions = new()
        {
            "push_to_main",
            "delete_files",
            "delete_database",
            "access_secrets",
            "modify_permissions",
            "execute_code",
            "bypass_authentication"
        };

        public string RuleName => "Forbidden Actions Block";
        public string Description => "Agent cannot declare forbidden high-risk actions";
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
                return Task.FromResult(result); // No actions declared is OK
            }

            var forbiddenDeclared = agent.Configuration.AllowedActions
                .Where(action => ForbiddenActions.Contains(action))
                .ToList();

            if (forbiddenDeclared.Any())
            {
                result.IsCompliant = false;
                foreach (var action in forbiddenDeclared)
                {
                    result.Violations.Add($"Action '{action}' is strictly forbidden");
                }
                result.Recommendations.Add("Remove all forbidden actions from the agent definition");
                result.Recommendations.Add($"Forbidden actions: {string.Join(", ", ForbiddenActions)}");
            }

            return Task.FromResult(result);
        }
    }
}
