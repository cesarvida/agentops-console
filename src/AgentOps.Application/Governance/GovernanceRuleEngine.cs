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
    /// Supports optional repo-specific <see cref="GovernanceConfig"/> for configurable
    /// allowed/forbidden lists and scoring thresholds, and active
    /// <see cref="GovernanceException"/> downgrades.
    /// </summary>
    public class GovernanceRuleEngine
    {
        private readonly IEnumerable<IGovernanceRule> _rules;

        public GovernanceRuleEngine(IEnumerable<IGovernanceRule> rules)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        /// <summary>
        /// Evaluates an agent using default governance configuration.
        /// </summary>
        public Task<GovernanceReport> EvaluateAsync(AgentDefinition agent)
            => EvaluateAsync(agent, GovernanceConfig.Default);

        /// <summary>
        /// Evaluates an agent using the provided governance configuration.
        /// </summary>
        /// <param name="agent">The agent definition to evaluate.</param>
        /// <param name="config">Repo-specific config; uses defaults when null.</param>
        public async Task<GovernanceReport> EvaluateAsync(AgentDefinition agent, GovernanceConfig? config)
        {
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));

            config ??= GovernanceConfig.Default;

            var report = new GovernanceReport
            {
                AgentId      = agent.Id.Value,
                AgentName    = agent.Name,
                AgentVersion = agent.Version,
                EvaluatedAt  = DateTime.UtcNow
            };

            // 1. Run all rules (config-aware where supported)
            var rawResults = await Task.WhenAll(
                _rules.Select(rule => rule is IConfigurableGovernanceRule cr
                    ? cr.EvaluateAsync(agent, config)
                    : rule.EvaluateAsync(agent))
            );

            // 2. Apply active exception downgrades (Critical → Warning)
            var ruleResults = ApplyExceptions(agent, rawResults);

            // 3. Collect results
            report.RuleResults.AddRange(ruleResults);

            // 4. Count violations
            report.CriticalViolations = ruleResults.Count(r => !r.IsCompliant && r.Severity == RuleSeverity.Critical);
            report.WarningViolations  = ruleResults.Count(r => !r.IsCompliant && r.Severity == RuleSeverity.Warning);

            // 5. Calculate score using config thresholds
            report.GovernanceScore = CalculateScore(
                report.CriticalViolations, report.WarningViolations, config.Scoring);

            // 6. Determine status using config thresholds
            report.IsCompliant  = report.CriticalViolations == 0;
            report.FinalStatus  = DetermineFinalStatus(
                report.GovernanceScore, report.CriticalViolations, config.Scoring);

            return report;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// For each Critical non-compliant result, checks if a valid
        /// <see cref="GovernanceException"/> exists in the agent's exception list.
        /// If yes, the result is downgraded to Warning and annotated.
        /// </summary>
        private static List<RuleResult> ApplyExceptions(AgentDefinition agent, RuleResult[] results)
        {
            var exceptions = agent.Exceptions;
            if (exceptions == null || exceptions.Count == 0)
                return new List<RuleResult>(results);

            var processed = new List<RuleResult>(results.Length);
            foreach (var result in results)
            {
                // Only downgrade non-compliant Critical violations
                if (result.IsCompliant || result.Severity != RuleSeverity.Critical)
                {
                    processed.Add(result);
                    continue;
                }

                var active = exceptions.FirstOrDefault(e =>
                    e.IsValid &&
                    string.Equals(e.RuleName, result.RuleName, StringComparison.OrdinalIgnoreCase));

                if (active == null)
                {
                    processed.Add(result);
                    continue;
                }

                // Clone and downgrade
                processed.Add(new RuleResult
                {
                    IsCompliant      = false,
                    RuleName         = result.RuleName,
                    Severity         = RuleSeverity.Warning,    // ← downgraded
                    Violations       = result.Violations,
                    Recommendations  = result.Recommendations,
                    ExceptionNote    = $"⚡ Excepción activa hasta {active.ExpiresAt:yyyy-MM-dd} — aprobada por {active.ApprovedBy}"
                });
            }

            return processed;
        }

        private static int CalculateScore(int critical, int warnings, ScoringConfig scoring)
        {
            int score = 100;
            score -= critical * scoring.CriticalPenalty;
            score -= warnings * scoring.WarningPenalty;
            return Math.Max(0, score);
        }

        private static string DetermineFinalStatus(int score, int critical, ScoringConfig scoring)
        {
            if (critical > 0 || score < scoring.BlockedThreshold)
                return "BLOCKED";
            if (score < scoring.ReviewThreshold)
                return "REVIEW";
            return "APPROVED";
        }
    }
}
