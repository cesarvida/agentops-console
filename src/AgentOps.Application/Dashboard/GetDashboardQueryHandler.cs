using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.Governance;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Governance;

namespace AgentOps.Application.Dashboard
{
    /// <summary>
    /// Handler for <see cref="GetDashboardQuery"/>.
    /// Fetches all agent definitions from GitHub, evaluates each with the governance engine,
    /// and returns a <see cref="DashboardResult"/> with per-agent rows and summary counts.
    /// </summary>
    public class GetDashboardQueryHandler
    {
        private readonly IAgentFetcher _fetcher;
        private readonly GovernanceRuleEngine _engine;
        private readonly IGovernanceConfigLoader _configLoader;

        public GetDashboardQueryHandler(
            IAgentFetcher fetcher,
            GovernanceRuleEngine engine,
            IGovernanceConfigLoader configLoader)
        {
            _fetcher      = fetcher       ?? throw new ArgumentNullException(nameof(fetcher));
            _engine       = engine        ?? throw new ArgumentNullException(nameof(engine));
            _configLoader = configLoader  ?? throw new ArgumentNullException(nameof(configLoader));
        }

        /// <summary>
        /// Executes the dashboard query.
        /// </summary>
        /// <param name="query">Query containing owner and repo.</param>
        /// <returns>Populated <see cref="DashboardResult"/>.</returns>
        public async Task<DashboardResult> HandleAsync(GetDashboardQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var result = new DashboardResult
            {
                Owner       = query.Owner,
                Repo        = query.Repo,
                GeneratedAt = DateTime.UtcNow
            };

            // 1. Load repo governance config
            var config = await _configLoader.LoadAsync(query.Owner, query.Repo);

            // 2. Fetch all agent definitions from the GitHub repo
            var agents = await _fetcher.FetchAgentsAsync(query.Owner, query.Repo);

            if (agents.Count == 0)
                return result;

            // 3. Evaluate each agent with the repo config
            var reports = await Task.WhenAll(agents.Select(a => _engine.EvaluateAsync(a, config)));

            // 4. Build per-agent rows
            foreach (var report in reports)
            {
                var violations = report.RuleResults
                    .Where(r => !r.IsCompliant)
                    .SelectMany(r => r.Violations)
                    .ToList();

                var exceptionNotes = report.RuleResults
                    .Where(r => !string.IsNullOrEmpty(r.ExceptionNote))
                    .Select(r => r.ExceptionNote!)
                    .ToList();

                result.Agents.Add(new AgentDashboardRow
                {
                    AgentName           = report.AgentName,
                    Version             = report.AgentVersion,
                    GovernanceScore     = report.GovernanceScore,
                    Status              = report.FinalStatus,
                    CriticalViolations  = report.CriticalViolations,
                    WarningViolations   = report.WarningViolations,
                    ViolationDetails    = violations,
                    HasActiveExceptions = report.HasExceptionOverrides,
                    ExceptionNotes      = exceptionNotes
                });
            }

            // 4. Aggregate counts
            result.TotalAgents   = result.Agents.Count;
            result.ApprovedCount = result.Agents.Count(a => a.Status == "APPROVED");
            result.ReviewCount   = result.Agents.Count(a => a.Status == "REVIEW");
            result.BlockedCount  = result.Agents.Count(a => a.Status == "BLOCKED");

            return result;
        }
    }
}
