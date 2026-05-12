using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;

namespace AgentOps.Application.Governance
{
    /// <summary>
    /// The main Governance Rule Engine that orchestrates all governance rule evaluations.
    /// </summary>
    public class GovernanceRuleEngine
    {
        private readonly IEnumerable<IGovernanceRule> _rules;

        public GovernanceRuleEngine(IEnumerable<IGovernanceRule> rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// Evaluates an agent definition against all governance rules.
        /// </summary>
        /// <param name="agent">The agent definition to evaluate.</param>
        /// <returns>A comprehensive governance report.</returns>
        public async Task<GovernanceReport> EvaluateAsync(AgentDefinition agent)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            var report = new GovernanceReport
            {
                AgentId = agent.Id.Value,
                AgentName = agent.Name,
                AgentVersion = agent.Version,
                EvaluatedAt = DateTime.UtcNow
            };

            // Run all rules in parallel for performance
            var ruleEvaluations = await Task.WhenAll(
                _rules.Select(rule => rule.EvaluateAsync(agent))
            );

            // Collect results
            report.RuleResults.AddRange(ruleEvaluations);

            // Count violations by severity
            report.CriticalViolations = ruleEvaluations.Count(r => !r.IsCompliant && r.Severity == RuleSeverity.Critical);
            report.WarningViolations = ruleEvaluations.Count(r => !r.IsCompliant && r.Severity == RuleSeverity.Warning);

            // Calculate governance score
            report.GovernanceScore = CalculateScore(report.CriticalViolations, report.WarningViolations);

            // Determine final status
            report.IsCompliant = report.CriticalViolations == 0;
            report.FinalStatus = DetermineFinalStatus(report.GovernanceScore, report.CriticalViolations);

            return report;
        }

        /// <summary>
        /// Calculates the governance score based on violations.
        /// Base score: 100
        /// Each critical violation: -25 points
        /// Each warning violation: -10 points
        /// Minimum score: 0
        /// </summary>
        private int CalculateScore(int criticalViolations, int warningViolations)
        {
            int score = 100;
            score -= (criticalViolations * 25);
            score -= (warningViolations * 10);
            return Math.Max(0, score);
        }

        /// <summary>
        /// Determines the final status based on score and critical violations.
        /// </summary>
        private string DetermineFinalStatus(int score, int criticalViolations)
        {
            // BLOCKED if critical violations or score too low
            if (criticalViolations > 0 || score < 40)
            {
                return "BLOCKED";
            }

            // REVIEW if score is medium
            if (score < 70)
            {
                return "REVIEW";
            }

            // APPROVED if score is high and no critical violations
            return "APPROVED";
        }
    }
}
