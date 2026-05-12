using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Rule that blocks agents from declaring forbidden actions.
    /// The forbidden set is taken from the repo's <see cref="GovernanceConfig"/> when available,
    /// falling back to the default set.
    /// </summary>
    public class ForbiddenActionsRule : IConfigurableGovernanceRule
    {
        private static readonly HashSet<string> DefaultForbidden =
            new(GovernanceConfig.DefaultForbiddenActions);

        public string RuleName  => "Forbidden Actions Block";
        public string Description => "Agent cannot declare forbidden high-risk actions";
        public RuleSeverity Severity => RuleSeverity.Critical;

        public Task<RuleResult> EvaluateAsync(AgentDefinition agent)
            => EvaluateAsync(agent, GovernanceConfig.Default);

        public Task<RuleResult> EvaluateAsync(AgentDefinition agent, GovernanceConfig config)
        {
            var forbidden = config.ForbiddenActions?.Count > 0
                ? new HashSet<string>(config.ForbiddenActions)
                : DefaultForbidden;

            var result = new RuleResult
            {
                RuleName = RuleName,
                Severity = Severity,
                IsCompliant = true,
                Violations = new(),
                Recommendations = new()
            };

            if (agent?.Configuration?.AllowedActions == null || !agent.Configuration.AllowedActions.Any())
                return Task.FromResult(result);

            var declared = agent.Configuration.AllowedActions
                .Where(a => forbidden.Contains(a))
                .ToList();

            if (declared.Any())
            {
                result.IsCompliant = false;
                foreach (var a in declared)
                    result.Violations.Add($"Action '{a}' is strictly forbidden");
                result.Recommendations.Add("Remove all forbidden actions from the agent definition");
                result.Recommendations.Add(
                    $"Forbidden actions: {string.Join(", ", forbidden)}");
            }

            return Task.FromResult(result);
        }
    }
}
