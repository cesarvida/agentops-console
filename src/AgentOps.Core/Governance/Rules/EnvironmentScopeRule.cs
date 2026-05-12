using System;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Critical-level rule that requires agents to declare valid deployment environments.
    /// Compliant: <c>environments</c> list is non-empty and contains only recognised values.
    /// Config-aware: if the agent declares "production" and the repo config marks production as
    /// requiring human approval, that is enforced as a violation.
    /// </summary>
    public class EnvironmentScopeRule : IConfigurableGovernanceRule
    {
        private static readonly string[] ValidEnvironments =
            { "development", "staging", "production" };

        public string RuleName    => "Environment Scope Defined";
        public string Description => "Agent must declare the environments it is allowed to run in";
        public RuleSeverity Severity => RuleSeverity.Critical;

        public Task<RuleResult> EvaluateAsync(AgentDefinition agent)
            => EvaluateAsync(agent, GovernanceConfig.Default);

        public Task<RuleResult> EvaluateAsync(AgentDefinition agent, GovernanceConfig config)
        {
            var result = new RuleResult
            {
                RuleName    = RuleName,
                Severity    = Severity,
                IsCompliant = true,
                Violations  = new(),
                Recommendations = new()
            };

            var envs = agent?.Configuration?.Environments;

            if (envs == null || envs.Count == 0)
            {
                result.IsCompliant = false;
                result.Violations.Add("No deployment environments are declared for this agent");
                result.Recommendations.Add(
                    $"Add 'environments' to the agent YAML. Valid values: {string.Join(", ", ValidEnvironments)}");
                return Task.FromResult(result);
            }

            // Check for unrecognised environment names
            var invalid = envs
                .Where(e => !ValidEnvironments.Contains(e, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (invalid.Any())
            {
                result.IsCompliant = false;
                foreach (var e in invalid)
                    result.Violations.Add($"Unknown environment '{e}'");
                result.Recommendations.Add(
                    $"Use only recognised values: {string.Join(", ", ValidEnvironments)}");
            }

            // Config-aware: production + require_human_approval enforcement
            bool declaresProduction = envs.Any(
                e => string.Equals(e, "production", StringComparison.OrdinalIgnoreCase));

            if (declaresProduction && config.Environments.TryGetValue("production", out var prodConfig))
            {
                if (prodConfig.RequireHumanApproval)
                {
                    // The repo policy requires human approval for production deployments.
                    // An agent that declares production must be flagged for review —
                    // this is a governance gate, not a blocker (downgraded to Warning here
                    // so that legitimate production agents can still be APPROVED after review).
                    // We add a violation but at Warning severity via a separate result note.
                    result.Violations.Add(
                        "Agent declares 'production' environment — human approval is required by repo governance policy");
                    result.Recommendations.Add(
                        "Ensure a human approver has reviewed and signed off this agent before deploying to production");
                    result.IsCompliant = false;
                }
            }

            return Task.FromResult(result);
        }
    }
}
