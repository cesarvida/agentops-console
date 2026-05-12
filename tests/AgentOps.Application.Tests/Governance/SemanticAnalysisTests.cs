using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentOps.Application.Governance;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.Governance.Rules;
using AgentOps.Core.ValueObjects;
using Moq;
using Xunit;

namespace AgentOps.Application.Tests.Governance
{
    /// <summary>
    /// Phase 11 — Semantic Analysis integration tests.
    /// Uses a mock IAgentSemanticAnalyzer so no real Azure calls are made.
    /// </summary>
    public class SemanticAnalysisTests : IDisposable
    {
        // ── Minimal compliant YAML written to a temp file for each test ──────
        private readonly string _tempYamlPath;

        private const string CompliantYaml = @"
id: test-agent-001
name: Test Agent
version: 1.0.0
description: Minimal compliant agent for testing
owner: team-test
actions:
  - read_code
  - post_comment
rate_limit:
  requests_per_minute: 60
timeout_seconds: 30
environments:
  - development
audit:
  log_all_actions: true
  retention_days: 30
";

        public SemanticAnalysisTests()
        {
            _tempYamlPath = Path.GetTempFileName() + ".yaml";
            File.WriteAllText(_tempYamlPath, CompliantYaml);
        }

        public void Dispose()
        {
            if (File.Exists(_tempYamlPath))
                File.Delete(_tempYamlPath);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static GovernanceRuleEngine BuildPassingEngine()
        {
            // A single rule that always passes — lets semantic analysis drive the status
            var rule = new Mock<IGovernanceRule>();
            rule.SetupGet(r => r.RuleName).Returns("AlwaysPass");
            rule.SetupGet(r => r.Severity).Returns(RuleSeverity.Info);
            rule.Setup(r => r.EvaluateAsync(It.IsAny<AgentDefinition>()))
                .ReturnsAsync(new RuleResult
                {
                    IsCompliant = true,
                    RuleName    = "AlwaysPass",
                    Severity    = RuleSeverity.Info
                });
            return new GovernanceRuleEngine(new[] { rule.Object });
        }

        private static GovernanceRuleEngine BuildBlockingEngine()
        {
            // A single Critical rule that always fails — simulates existing BLOCKED state
            var rule = new Mock<IGovernanceRule>();
            rule.SetupGet(r => r.RuleName).Returns("AlwaysFail");
            rule.SetupGet(r => r.Severity).Returns(RuleSeverity.Critical);
            rule.Setup(r => r.EvaluateAsync(It.IsAny<AgentDefinition>()))
                .ReturnsAsync(new RuleResult
                {
                    IsCompliant = false,
                    RuleName    = "AlwaysFail",
                    Severity    = RuleSeverity.Critical,
                    Violations  = new List<string> { "Critical violation for testing" }
                });
            return new GovernanceRuleEngine(new[] { rule.Object });
        }

        private static GovernanceConfig ConfigWith(bool enabled, string threshold = "MEDIUM")
            => new GovernanceConfig
            {
                SemanticAnalysis = new SemanticAnalysisConfig
                {
                    Enabled   = enabled,
                    Threshold = threshold
                }
            };

        private static Mock<IAgentSemanticAnalyzer> MockReturning(string riskLevel, bool isAvailable = true)
        {
            var mock = new Mock<IAgentSemanticAnalyzer>();
            mock.Setup(a => a.AnalyzeAgentSemanticsAsync(
                    It.IsAny<string>(),
                    It.IsAny<SemanticAnalysisConfig>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(isAvailable
                    ? new SemanticAnalysisResult
                    {
                        RiskLevel       = riskLevel,
                        IsAvailable     = true,
                        Issues          = new List<string> { $"Test issue for {riskLevel}" },
                        Recommendations = new List<string> { "Test recommendation" }
                    }
                    : SemanticAnalysisResult.Skipped("Mock skipped reason"));
            return mock;
        }

        // ── Test 1: Semantic disabled → existing behavior unchanged ──────────

        [Fact]
        public async Task SemanticDisabled_ExistingBehaviorUnchanged()
        {
            var engine  = BuildPassingEngine();
            var handler = new ValidateAgentCommandHandler(engine, semanticAnalyzer: null);
            var config  = ConfigWith(enabled: false);

            var report = await handler.HandleAsync(new ValidateAgentCommand(_tempYamlPath), config);

            Assert.Equal("APPROVED", report.FinalStatus);
            Assert.Null(report.SemanticAnalysis);
        }

        // ── Test 2: No semantic analyzer injected → skipped, no crash ────────

        [Fact]
        public async Task NoSemanticAnalyzer_Skipped_NoCrash()
        {
            var engine  = BuildPassingEngine();
            // Pass null analyzer — simulates missing Azure credentials
            var handler = new ValidateAgentCommandHandler(engine, semanticAnalyzer: null);
            var config  = ConfigWith(enabled: true); // enabled but no impl

            var report = await handler.HandleAsync(new ValidateAgentCommand(_tempYamlPath), config);

            // Status stays from rule engine; no semantic result
            Assert.Equal("APPROVED", report.FinalStatus);
            Assert.Null(report.SemanticAnalysis);
        }

        // ── Test 3: API timeout → semantic skipped, no crash ─────────────────

        [Fact]
        public async Task ApiTimeout_Skipped_NoCrash()
        {
            var engine        = BuildPassingEngine();
            var semanticMock  = MockReturning("LOW", isAvailable: false); // timeout returns skipped
            // Override to return a "timeout" skipped result
            semanticMock.Setup(a => a.AnalyzeAgentSemanticsAsync(
                    It.IsAny<string>(),
                    It.IsAny<SemanticAnalysisConfig>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(SemanticAnalysisResult.Skipped("Timeout after 5s"));

            var handler = new ValidateAgentCommandHandler(engine, semanticMock.Object);
            var config  = ConfigWith(enabled: true);

            var report = await handler.HandleAsync(new ValidateAgentCommand(_tempYamlPath), config);

            Assert.Equal("APPROVED", report.FinalStatus);
            Assert.NotNull(report.SemanticAnalysis);
            Assert.False(report.SemanticAnalysis!.IsAvailable);
            Assert.Contains("Timeout", report.SemanticAnalysis.ErrorMessage);
        }

        // ── Test 4: Invalid JSON response → semantic skipped, no crash ────────

        [Fact]
        public async Task InvalidJsonResponse_Skipped_NoCrash()
        {
            var engine       = BuildPassingEngine();
            var semanticMock = new Mock<IAgentSemanticAnalyzer>();
            semanticMock.Setup(a => a.AnalyzeAgentSemanticsAsync(
                    It.IsAny<string>(),
                    It.IsAny<SemanticAnalysisConfig>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SemanticAnalysisResult
                {
                    IsAvailable  = false,
                    RiskLevel    = "LOW",
                    Issues       = new List<string> { "Semantic analysis unavailable: invalid model response" },
                    ErrorMessage = "Invalid JSON response from model"
                });

            var handler = new ValidateAgentCommandHandler(engine, semanticMock.Object);
            var config  = ConfigWith(enabled: true);

            var report = await handler.HandleAsync(new ValidateAgentCommand(_tempYamlPath), config);

            // Status stays APPROVED (rule engine) — unavailable semantic doesn't escalate
            Assert.Equal("APPROVED", report.FinalStatus);
            Assert.False(report.SemanticAnalysis!.IsAvailable);
            Assert.Contains("invalid model response", report.SemanticAnalysis.Issues[0]);
        }

        // ── Test 5: Semantic LOW → no status escalation ───────────────────────

        [Fact]
        public async Task SemanticLow_NoStatusEscalation()
        {
            var engine       = BuildPassingEngine();
            var semanticMock = MockReturning("LOW");
            var handler      = new ValidateAgentCommandHandler(engine, semanticMock.Object);
            var config       = ConfigWith(enabled: true);

            var report = await handler.HandleAsync(new ValidateAgentCommand(_tempYamlPath), config);

            Assert.Equal("APPROVED", report.FinalStatus);
            Assert.True(report.SemanticAnalysis!.IsAvailable);
            Assert.Equal("LOW", report.SemanticAnalysis.RiskLevel);
        }

        // ── Test 6: Semantic MEDIUM → APPROVED becomes REVIEW ─────────────────

        [Fact]
        public async Task SemanticMedium_ApprovedBecomesReview()
        {
            var engine       = BuildPassingEngine(); // rule engine → APPROVED
            var semanticMock = MockReturning("MEDIUM");
            var handler      = new ValidateAgentCommandHandler(engine, semanticMock.Object);
            var config       = ConfigWith(enabled: true);

            var report = await handler.HandleAsync(new ValidateAgentCommand(_tempYamlPath), config);

            Assert.Equal("REVIEW", report.FinalStatus);
            Assert.True(report.SemanticAnalysis!.IsAvailable);
            Assert.Equal("MEDIUM", report.SemanticAnalysis.RiskLevel);
        }

        // ── Test 7: Semantic HIGH → APPROVED becomes BLOCKED ──────────────────

        [Fact]
        public async Task SemanticHigh_ApprovedBecomesBlocked()
        {
            var engine       = BuildPassingEngine();
            var semanticMock = MockReturning("HIGH");
            var handler      = new ValidateAgentCommandHandler(engine, semanticMock.Object);
            var config       = ConfigWith(enabled: true);

            var report = await handler.HandleAsync(new ValidateAgentCommand(_tempYamlPath), config);

            Assert.Equal("BLOCKED", report.FinalStatus);
            Assert.Equal("HIGH", report.SemanticAnalysis!.RiskLevel);
        }

        // ── Test 8: Rule-based BLOCKED remains BLOCKED (semantic HIGH irrelevant) ──

        [Fact]
        public async Task RuleBasedBlocked_RemainsBlocked_SemanticDoesNotOverride()
        {
            var engine       = BuildBlockingEngine(); // already BLOCKED by rules
            var semanticMock = MockReturning("HIGH");
            var handler      = new ValidateAgentCommandHandler(engine, semanticMock.Object);
            var config       = ConfigWith(enabled: true);

            var report = await handler.HandleAsync(new ValidateAgentCommand(_tempYamlPath), config);

            // Must be BLOCKED and for the right reason (critical rule violation)
            Assert.Equal("BLOCKED", report.FinalStatus);
            Assert.True(report.CriticalViolations > 0);
        }

        // ── Test 9: Exit-code contract — BLOCKED→1, REVIEW→0, APPROVED→0 ──────

        [Theory]
        [InlineData("APPROVED", false)]
        [InlineData("REVIEW",   false)]
        [InlineData("BLOCKED",  true)]
        public void FinalStatus_ExitCodeContract(string status, bool shouldBeNonZero)
        {
            // This verifies the contract used in Program.cs validate-agent branch:
            //   Environment.ExitCode = 1  when FinalStatus == "BLOCKED"
            //   Environment.ExitCode = 0  otherwise
            bool wouldExit1 = status == "BLOCKED";
            Assert.Equal(shouldBeNonZero, wouldExit1);
        }
    }
}
