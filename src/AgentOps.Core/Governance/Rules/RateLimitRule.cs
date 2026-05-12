using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Warning-level rule that requires agents to declare a valid rate limit.
    /// Compliant: 1 ≤ requests_per_minute ≤ 1000.
    /// Non-compliant (Warning): missing rate limit, or rate limit > 1000.
    /// </summary>
    public class RateLimitRule : IGovernanceRule
    {
        private const int MaxAllowedRpm = 1000;

        public string RuleName    => "Rate Limit Defined";
        public string Description => "Agent must declare a rate limit between 1 and 1000 requests/minute";
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

            var rpm = agent?.Configuration?.RateLimitRequestsPerMinute;

            if (rpm == null)
            {
                result.IsCompliant = false;
                result.Violations.Add("No rate limit is defined for this agent");
                result.Recommendations.Add("Add 'rate_limit.requests_per_minute' to the agent YAML (recommended: 60)");
                return Task.FromResult(result);
            }

            if (rpm.Value <= 0)
            {
                result.IsCompliant = false;
                result.Violations.Add($"Rate limit must be greater than 0 (got {rpm.Value})");
                result.Recommendations.Add("Set 'rate_limit.requests_per_minute' to a positive value");
                return Task.FromResult(result);
            }

            if (rpm.Value > MaxAllowedRpm)
            {
                result.IsCompliant = false;
                result.Violations.Add(
                    $"Rate limit of {rpm.Value} req/min is too high (max allowed: {MaxAllowedRpm}) — possible abuse risk");
                result.Recommendations.Add(
                    $"Lower 'rate_limit.requests_per_minute' to {MaxAllowedRpm} or less");
            }

            return Task.FromResult(result);
        }
    }
}
