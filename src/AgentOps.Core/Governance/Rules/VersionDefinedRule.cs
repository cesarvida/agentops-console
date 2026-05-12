using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance.Rules
{
    /// <summary>
    /// Rule that validates agents have a valid semantic version defined.
    /// Warning-level: doesn't block but lowers the score.
    /// </summary>
    public class VersionDefinedRule : IGovernanceRule
    {
        private static readonly Regex SemanticVersionPattern = new(@"^\d+\.\d+\.\d+$");

        public string RuleName => "Version Defined";
        public string Description => "Agent should have a valid semantic version (X.Y.Z format)";
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

            if (string.IsNullOrWhiteSpace(agent?.Version))
            {
                result.IsCompliant = false;
                result.Violations.Add("Agent version is not defined");
                result.Recommendations.Add("Add version in semantic format: X.Y.Z");
                return Task.FromResult(result);
            }

            string version = agent.Version.Trim().ToLowerInvariant();

            // Check for invalid formats
            if (version == "latest" || version == "dev" || version == "master")
            {
                result.IsCompliant = false;
                result.Violations.Add($"Invalid version format: '{version}' is not allowed");
                result.Recommendations.Add("Use semantic versioning: 1.0.0, 1.2.3, etc.");
                return Task.FromResult(result);
            }

            // Validate semantic version pattern
            if (!SemanticVersionPattern.IsMatch(agent.Version))
            {
                result.IsCompliant = false;
                result.Violations.Add($"Version '{agent.Version}' does not match semantic versioning (X.Y.Z)");
                result.Recommendations.Add("Use format like: 1.0.0, 2.3.1, etc.");
            }

            return Task.FromResult(result);
        }
    }
}
