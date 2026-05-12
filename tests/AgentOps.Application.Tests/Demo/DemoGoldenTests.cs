using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentOps.Application.Governance;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Governance;
using AgentOps.Core.Governance.Rules;
using Xunit;

namespace AgentOps.Application.Tests.Demo
{
    /// <summary>
    /// Phase 12 — Demo golden tests.
    /// Validates the three demo agent scenarios (approved / review / blocked) using
    /// the real rule set and a deterministic FakeAgentSemanticAnalyzer so the tests
    /// run without any network dependency.
    ///
    /// Key invariants:
    ///   approved-agent → APPROVED, no criticals
    ///   review-agent   → REVIEW (warnings only + MEDIUM semantic escalation)
    ///   blocked-agent  → BLOCKED (critical violations, semantic cannot rescue)
    /// </summary>
    public class DemoGoldenTests : IDisposable
    {
        // ── Agent YAML strings (mirrors demo/agents/) ─────────────────────────

        private const string ApprovedAgentYaml = @"
id: demo-approved-001
name: Demo Approved Agent
version: 1.0.0
description: Minimal fully compliant agent used in governance demo
owner: team-demo
actions:
  - read_code
  - read_files
  - post_comment
  - create_report
rate_limit:
  requests_per_minute: 60
timeout_seconds: 30
environments:
  - development
  - staging
audit:
  log_all_actions: true
  retention_days: 90
";

        private const string ReviewAgentYaml = @"
id: demo-review-002
name: Demo Review Agent
version: 2.0.0
description: Agent with overly high rate limit and timeout - triggers warnings
owner: team-demo
actions:
  - read_code
  - read_files
  - post_comment
  - create_report
  - send_notification
rate_limit:
  requests_per_minute: 2000
timeout_seconds: 400
environments:
  - development
audit:
  log_all_actions: true
  retention_days: 30
";

        private const string BlockedAgentYaml = @"
id: demo-blocked-003
name: Demo Blocked Agent
version: dev
description: Agent with forbidden actions - triggers critical violations
owner: """"
actions:
  - push_to_main
  - delete_files
  - access_secrets
  - read_code
audit:
  log_all_actions: false
  retention_days: 0
";

        // ── Temp files ────────────────────────────────────────────────────────

        private readonly string _approvedPath;
        private readonly string _reviewPath;
        private readonly string _blockedPath;

        public DemoGoldenTests()
        {
            _approvedPath = Path.GetTempFileName() + ".yaml";
            _reviewPath   = Path.GetTempFileName() + ".yaml";
            _blockedPath  = Path.GetTempFileName() + ".yaml";

            File.WriteAllText(_approvedPath, ApprovedAgentYaml);
            File.WriteAllText(_reviewPath,   ReviewAgentYaml);
            File.WriteAllText(_blockedPath,  BlockedAgentYaml);
        }

        public void Dispose()
        {
            foreach (var f in new[] { _approvedPath, _reviewPath, _blockedPath })
                if (File.Exists(f)) File.Delete(f);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GovernanceRuleEngine BuildFullEngine() =>
            new GovernanceRuleEngine(new IGovernanceRule[]
            {
                new AllowedActionsRule(),
                new ForbiddenActionsRule(),
                new AuditLoggingRule(),
                new OwnerDefinedRule(),
                new VersionDefinedRule(),
                new RateLimitRule(),
                new TimeoutRule(),
                new EnvironmentScopeRule(),
            });

        private static GovernanceConfig BuildSemanticConfig(bool enabled = true) =>
            new GovernanceConfig
            {
                SemanticAnalysis = new SemanticAnalysisConfig
                {
                    Enabled        = enabled,
                    Threshold      = "MEDIUM",
                    TimeoutSeconds = 5,
                    MaxTokens      = 800
                }
            };

        private static IAgentSemanticAnalyzer FakeAnalyzer(string riskLevel) =>
            new FakeAnalyzerStub(riskLevel);

        // ── Tests ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task ApprovedAgent_WithLowSemanticRisk_ReturnsApproved()
        {
            var handler = new ValidateAgentCommandHandler(BuildFullEngine(), FakeAnalyzer("LOW"));
            var report  = await handler.HandleAsync(new ValidateAgentCommand(_approvedPath), BuildSemanticConfig());

            Assert.Equal("APPROVED", report.FinalStatus);
            Assert.Equal(0, report.CriticalViolations);
            Assert.Equal(0, report.WarningViolations);
            Assert.True(report.GovernanceScore > 90,
                $"Expected score > 90 but was {report.GovernanceScore}");
            Assert.NotNull(report.SemanticAnalysis);
            Assert.Equal("LOW", report.SemanticAnalysis!.RiskLevel);
        }

        [Fact]
        public async Task ApprovedAgent_HasNoSemanticIssues()
        {
            var handler = new ValidateAgentCommandHandler(BuildFullEngine(), FakeAnalyzer("LOW"));
            var report  = await handler.HandleAsync(new ValidateAgentCommand(_approvedPath), BuildSemanticConfig());

            Assert.True(report.SemanticAnalysis!.IsAvailable);
            Assert.Empty(report.SemanticAnalysis.Issues);
        }

        [Fact]
        public async Task ReviewAgent_WithMediumSemanticRisk_ReturnsReview()
        {
            // Rule-based: score drops due to warnings (rate limit > 1000, timeout > 300)
            // Semantic MEDIUM escalates APPROVED → REVIEW
            var handler = new ValidateAgentCommandHandler(BuildFullEngine(), FakeAnalyzer("MEDIUM"));
            var report  = await handler.HandleAsync(new ValidateAgentCommand(_reviewPath), BuildSemanticConfig());

            Assert.Equal("REVIEW", report.FinalStatus);
            Assert.Equal(0, report.CriticalViolations);
            Assert.True(report.WarningViolations > 0,
                "Expected at least one warning for high rate_limit / timeout");
        }

        [Fact]
        public async Task ReviewAgent_SemanticResultIsAttached()
        {
            var handler = new ValidateAgentCommandHandler(BuildFullEngine(), FakeAnalyzer("MEDIUM"));
            var report  = await handler.HandleAsync(new ValidateAgentCommand(_reviewPath), BuildSemanticConfig());

            Assert.NotNull(report.SemanticAnalysis);
            Assert.Equal("MEDIUM", report.SemanticAnalysis!.RiskLevel);
            Assert.True(report.SemanticAnalysis.IsAvailable);
        }

        [Fact]
        public async Task BlockedAgent_WithHighSemanticRisk_RemainsBlocked()
        {
            // Critical forbidden actions → BLOCKED at rule level.
            // Semantic HIGH must not change the outcome.
            var handler = new ValidateAgentCommandHandler(BuildFullEngine(), FakeAnalyzer("HIGH"));
            var report  = await handler.HandleAsync(new ValidateAgentCommand(_blockedPath), BuildSemanticConfig());

            Assert.Equal("BLOCKED", report.FinalStatus);
            Assert.True(report.CriticalViolations > 0,
                "Expected at least one critical violation for forbidden actions");
        }

        [Fact]
        public async Task BlockedAgent_ExitCodeContractHolds()
        {
            // Validates that FinalStatus == BLOCKED is the signal the CLI uses for exit code 1
            var handler = new ValidateAgentCommandHandler(BuildFullEngine(), FakeAnalyzer("HIGH"));
            var report  = await handler.HandleAsync(new ValidateAgentCommand(_blockedPath), BuildSemanticConfig());

            // The CLI does: if (report.FinalStatus == "BLOCKED") Environment.ExitCode = 1
            Assert.Equal("BLOCKED", report.FinalStatus);
        }

        [Fact]
        public async Task BlockedAgent_SemanticCannotEscalateToApproved()
        {
            // Even with LOW semantic (best case), BLOCKED rule result must remain BLOCKED
            var handler = new ValidateAgentCommandHandler(BuildFullEngine(), FakeAnalyzer("LOW"));
            var report  = await handler.HandleAsync(new ValidateAgentCommand(_blockedPath), BuildSemanticConfig());

            Assert.Equal("BLOCKED", report.FinalStatus);
        }

        [Fact]
        public async Task AllThreeAgents_ProduceExpectedStatusSequence()
        {
            var engine = BuildFullEngine();
            var config = BuildSemanticConfig();

            var approved = await new ValidateAgentCommandHandler(engine, FakeAnalyzer("LOW"))
                               .HandleAsync(new ValidateAgentCommand(_approvedPath), config);
            var review   = await new ValidateAgentCommandHandler(engine, FakeAnalyzer("MEDIUM"))
                               .HandleAsync(new ValidateAgentCommand(_reviewPath), config);
            var blocked  = await new ValidateAgentCommandHandler(engine, FakeAnalyzer("HIGH"))
                               .HandleAsync(new ValidateAgentCommand(_blockedPath), config);

            Assert.Equal("APPROVED", approved.FinalStatus);
            Assert.Equal("REVIEW",   review.FinalStatus);
            Assert.Equal("BLOCKED",  blocked.FinalStatus);
        }

        // ── Inline fake analyzer (no dependency on demo project) ──────────────

        private sealed class FakeAnalyzerStub : IAgentSemanticAnalyzer
        {
            private readonly string _riskLevel;

            public FakeAnalyzerStub(string riskLevel) =>
                _riskLevel = riskLevel.ToUpperInvariant();

            public Task<SemanticAnalysisResult> AnalyzeAgentSemanticsAsync(
                string agentYaml, SemanticAnalysisConfig config,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(new SemanticAnalysisResult
                {
                    RiskLevel       = _riskLevel,
                    IsAvailable     = true,
                    Issues          = _riskLevel == "HIGH"   ? new List<string> { "Overly broad permissions" }
                                    : _riskLevel == "MEDIUM" ? new List<string> { "Rate limit ambiguous" }
                                    : new List<string>(),
                    Recommendations = new List<string>()
                });
        }
    }
}
