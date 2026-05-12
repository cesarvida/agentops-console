using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Warning-level rule that requires agents to declare a request timeout.
    /// Compliant: 1 ≤ timeout_seconds ≤ 300 (5 minutes).
    /// Non-compliant (Warning): missing timeout, or timeout > 300.
    /// </summary>
    public class TimeoutRule : IGovernanceRule
    {
        private const int MaxTimeoutSeconds = 300;

        public string RuleName    => "Timeout Defined";
        public string Description => "Agent must declare a request timeout between 1 and 300 seconds";
        public RuleSeverity Severity => RuleSeverity.Warning;

        public Task<RuleResult> EvaluateAsync(AgentDefinition agent)
        {
            var result = new RuleResult
            {
                RuleName    = RuleName,
                Severity    = Severity,
                IsCompliant = true,
                Violations  = new(),
                Recommendations = new()
            };

            var timeout = agent?.Configuration?.TimeoutSeconds;

            if (timeout == null)
            {
                result.IsCompliant = false;
                result.Violations.Add("No timeout is defined for this agent");
                result.Recommendations.Add("Add 'timeout_seconds' to the agent YAML (recommended: 30)");
                return Task.FromResult(result);
            }

            if (timeout.Value <= 0)
            {
                result.IsCompliant = false;
                result.Violations.Add($"Timeout must be greater than 0 seconds (got {timeout.Value})");
                result.Recommendations.Add("Set 'timeout_seconds' to a positive value");
                return Task.FromResult(result);
            }

            if (timeout.Value > MaxTimeoutSeconds)
            {
                result.IsCompliant = false;
                result.Violations.Add(
                    $"Timeout of {timeout.Value}s exceeds the maximum allowed ({MaxTimeoutSeconds}s / 5 min)");
                result.Recommendations.Add(
                    $"Lower 'timeout_seconds' to {MaxTimeoutSeconds} or less");
            }

            return Task.FromResult(result);
        }
    }
}
