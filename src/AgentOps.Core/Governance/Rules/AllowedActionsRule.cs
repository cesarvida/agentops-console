using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Rule that validates agents only declare allowed actions from a whitelist.
    /// The whitelist is taken from the repo's <see cref="GovernanceConfig"/> when available,
    /// falling back to the default set.
    /// </summary>
    public class AllowedActionsRule : IConfigurableGovernanceRule
    {
        private static readonly HashSet<string> DefaultAllowed =
            new(GovernanceConfig.DefaultAllowedActions);

        public string RuleName  => "Allowed Actions Whitelist";
        public string Description => "Agent can only declare actions from the allowed whitelist";
        public RuleSeverity Severity => RuleSeverity.Critical;

        public Task<RuleResult> EvaluateAsync(AgentDefinition agent)
            => EvaluateAsync(agent, GovernanceConfig.Default);

        public Task<RuleResult> EvaluateAsync(AgentDefinition agent, GovernanceConfig config)
        {
            var allowed = config.AllowedActions?.Count > 0
                ? new HashSet<string>(config.AllowedActions)
                : DefaultAllowed;

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

            var invalid = agent.Configuration.AllowedActions
                .Where(a => !allowed.Contains(a))
                .ToList();

            if (invalid.Any())
            {
                result.IsCompliant = false;
                foreach (var a in invalid)
                    result.Violations.Add($"Action '{a}' is not in the allowed whitelist");
                result.Recommendations.Add(
                    $"Remove or replace with one of these actions: {string.Join(", ", allowed)}");
            }

            return Task.FromResult(result);
        }
    }
}
