using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Core.Governance
{
    /// <summary>
    /// Severity level for governance rule violations.
    /// </summary>
    public enum RuleSeverity
    {
        /// <summary>Critical - Blocks the PR</summary>
        Critical,
        /// <summary>Warning - Lowers the score but doesn't block</summary>
        Warning,
        /// <summary>Info - Informational only</summary>
        Info
    }

    /// <summary>
    /// Result of a single governance rule evaluation.
    /// </summary>
    public class RuleResult
    {
        /// <summary>Indicates if the agent complies with this rule.</summary>
        public bool IsCompliant { get; set; }

        /// <summary>Name of the rule that was evaluated.</summary>
        public string RuleName { get; set; } = string.Empty;

        /// <summary>Severity level of this rule.</summary>
        public RuleSeverity Severity { get; set; }

        /// <summary>List of specific violations found.</summary>
        public List<string> Violations { get; set; } = new();

        /// <summary>List of recommendations to fix the violations.</summary>
        public List<string> Recommendations { get; set; } = new();

        /// <summary>
        /// When a <see cref="GovernanceException"/> is active and has downgraded this result
        /// from Critical to Warning, this property carries a human-readable note.
        /// Null otherwise.
        /// </summary>
        public string? ExceptionNote { get; set; }
    }

    /// <summary>
    /// Interface for governance rules that validate agent definitions.
    /// </summary>
    public interface IGovernanceRule
    {
        /// <summary>Gets the name of this rule.</summary>
        string RuleName { get; }

        /// <summary>Gets the description of what this rule validates.</summary>
        string Description { get; }

        /// <summary>Gets the severity level of violations for this rule.</summary>
        RuleSeverity Severity { get; }

        /// <summary>
        /// Evaluates an agent definition against this rule.
        /// </summary>
        /// <param name="agent">The agent definition to evaluate.</param>
        /// <returns>A RuleResult indicating compliance and any violations.</returns>
        Task<RuleResult> EvaluateAsync(AgentDefinition agent);
    }

    /// <summary>
    /// Extended interface for rules that can use repo-specific <see cref="GovernanceConfig"/>
    /// to override their default allowed/forbidden lists and thresholds.
    /// Rules that implement this interface are called with the loaded config by
    /// <see cref="AgentOps.Application.Governance.GovernanceRuleEngine"/>.
    /// </summary>
    public interface IConfigurableGovernanceRule : IGovernanceRule
    {
        /// <summary>
        /// Evaluates an agent definition using the provided governance configuration.
        /// </summary>
        Task<RuleResult> EvaluateAsync(AgentDefinition agent, GovernanceConfig config);
    }
}
