using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.Dashboard;
using AgentOps.Application.Governance;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.ValueObjects;
using Moq;
using Xunit;

namespace AgentOps.Application.Tests.Dashboard
{
    public class GetDashboardQueryTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────

        private static AgentDefinition BuildAgent(string name, string version = "1.0.0")
            => new AgentDefinition(
                new AgentId(Guid.NewGuid().ToString()),
                name,
                $"Test agent '{name}' for dashboard tests",
                purpose: "Dashboard testing",
                rules: new List<string> { "rule" },
                tools: new List<string> { "tool" },
                configuration: new AgentConfiguration { Owner = "owner" },
                createdAt: DateTime.UtcNow,
                version: version);

        /// <summary>
        /// Builds a GovernanceRuleEngine whose single mock rule returns a compliant
        /// result for every agent.
        /// </summary>
        private static (GovernanceRuleEngine engine, Mock<IAgentFetcher> fetcherMock)
            BuildEngine(
                List<AgentDefinition> agents,
                Func<Mock<IGovernanceRule>, string, string> configureRule)
        {
            // Rule that returns non-compliant results keyed by agent name
            var ruleMock = new Mock<IGovernanceRule>();
            ruleMock.SetupGet(r => r.RuleName).Returns("TestRule");
            return (new GovernanceRuleEngine(new[] { ruleMock.Object }),
                    new Mock<IAgentFetcher>());
        }

        /// <summary>Stub IGovernanceConfigLoader that always returns the default config.</summary>
        private static Mock<IGovernanceConfigLoader> DefaultConfigLoader()
        {
            var mock = new Mock<IGovernanceConfigLoader>();
            mock.Setup(l => l.LoadAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(GovernanceConfig.Default);
            return mock;
        }

        // ── Test 1: Three agents with distinct statuses ───────────────────────

        /// <summary>
        /// Dashboard with 3 agents (APPROVED, REVIEW, BLOCKED) returns correct summary counts.
        /// </summary>
        [Fact]
        public async Task HandleAsync_ThreeAgentsWithDifferentStatuses_ReturnsCorrectCounts()
        {
            // Arrange ─ agents
            var approvedAgent = BuildAgent("ApprovedAgent");
            var reviewAgent   = BuildAgent("ReviewAgent");
            var blockedAgent  = BuildAgent("BlockedAgent");

            var mockFetcher = new Mock<IAgentFetcher>();
            mockFetcher
                .Setup(f => f.FetchAgentsAsync("owner", "repo"))
                .ReturnsAsync(new List<AgentDefinition> { approvedAgent, reviewAgent, blockedAgent });

            // 4 warning rules — all fire only for "ReviewAgent" → score 60 → REVIEW
            var warningRules = Enumerable.Range(1, 4).Select(i =>
            {
                var m = new Mock<IGovernanceRule>();
                m.SetupGet(r => r.Severity).Returns(RuleSeverity.Warning);
                m.SetupGet(r => r.RuleName).Returns($"WarnRule{i}");
                m.Setup(r => r.EvaluateAsync(It.Is<AgentDefinition>(a => a.Name == "ReviewAgent")))
                 .ReturnsAsync(new RuleResult
                 {
                     IsCompliant = false,
                     RuleName    = $"WarnRule{i}",
                     Severity    = RuleSeverity.Warning,
                     Violations  = new List<string> { $"warning {i}" }
                 });
                m.Setup(r => r.EvaluateAsync(It.Is<AgentDefinition>(a => a.Name != "ReviewAgent")))
                 .ReturnsAsync(new RuleResult { IsCompliant = true, RuleName = $"WarnRule{i}", Severity = RuleSeverity.Warning });
                return m;
            }).ToList();

            // 1 critical rule — fires only for "BlockedAgent" → BLOCKED
            var criticalRule = new Mock<IGovernanceRule>();
            criticalRule.SetupGet(r => r.Severity).Returns(RuleSeverity.Critical);
            criticalRule.SetupGet(r => r.RuleName).Returns("CriticalRule");
            criticalRule
                .Setup(r => r.EvaluateAsync(It.Is<AgentDefinition>(a => a.Name == "BlockedAgent")))
                .ReturnsAsync(new RuleResult
                {
                    IsCompliant = false,
                    RuleName    = "CriticalRule",
                    Severity    = RuleSeverity.Critical,
                    Violations  = new List<string> { "critical violation" }
                });
            criticalRule
                .Setup(r => r.EvaluateAsync(It.Is<AgentDefinition>(a => a.Name != "BlockedAgent")))
                .ReturnsAsync(new RuleResult { IsCompliant = true, RuleName = "CriticalRule", Severity = RuleSeverity.Critical });

            var allRules = warningRules.Select(m => m.Object)
                                       .Append(criticalRule.Object);
            var engine  = new GovernanceRuleEngine(allRules);
            var handler = new GetDashboardQueryHandler(mockFetcher.Object, engine, DefaultConfigLoader().Object);

            // Act
            var result = await handler.HandleAsync(new GetDashboardQuery("owner", "repo"));

            // Assert
            Assert.Equal(3, result.TotalAgents);
            Assert.Equal(1, result.ApprovedCount);   // ApprovedAgent → 0 violations
            Assert.Equal(1, result.ReviewCount);     // ReviewAgent  → 4 warnings, score=60
            Assert.Equal(1, result.BlockedCount);    // BlockedAgent → 1 critical
        }

        // ── Test 2: Empty repo ────────────────────────────────────────────────

        /// <summary>
        /// Dashboard for a repo with no agent definitions returns an empty result without throwing.
        /// </summary>
        [Fact]
        public async Task HandleAsync_EmptyRepo_ReturnEmptyResultWithoutException()
        {
            // Arrange
            var mockFetcher = new Mock<IAgentFetcher>();
            mockFetcher
                .Setup(f => f.FetchAgentsAsync("empty-owner", "empty-repo"))
                .ReturnsAsync(new List<AgentDefinition>());

            var passRule = new Mock<IGovernanceRule>();
            passRule.SetupGet(r => r.RuleName).Returns("NopRule");
            passRule.Setup(r => r.EvaluateAsync(It.IsAny<AgentDefinition>()))
                    .ReturnsAsync(new RuleResult { IsCompliant = true, RuleName = "NopRule", Severity = RuleSeverity.Info });

            var engine  = new GovernanceRuleEngine(new[] { passRule.Object });
            var handler = new GetDashboardQueryHandler(mockFetcher.Object, engine, DefaultConfigLoader().Object);

            // Act — must not throw
            var result = await handler.HandleAsync(new GetDashboardQuery("empty-owner", "empty-repo"));

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalAgents);
            Assert.Equal(0, result.ApprovedCount);
            Assert.Equal(0, result.ReviewCount);
            Assert.Equal(0, result.BlockedCount);
            Assert.Empty(result.Agents);
        }

        // ── Test 3: APPROVED agent has 0 critical violations ─────────────────

        /// <summary>
        /// An agent that passes all rules must have CriticalViolations == 0 in the dashboard row.
        /// </summary>
        [Fact]
        public async Task HandleAsync_ApprovedAgent_HasZeroCriticalViolations()
        {
            // Arrange
            var agent = BuildAgent("GoodAgent");

            var mockFetcher = new Mock<IAgentFetcher>();
            mockFetcher
                .Setup(f => f.FetchAgentsAsync("o", "r"))
                .ReturnsAsync(new List<AgentDefinition> { agent });

            var passRule = new Mock<IGovernanceRule>();
            passRule.SetupGet(r => r.RuleName).Returns("PassRule");
            passRule.SetupGet(r => r.Severity).Returns(RuleSeverity.Critical);
            passRule.Setup(r => r.EvaluateAsync(It.IsAny<AgentDefinition>()))
                    .ReturnsAsync(new RuleResult { IsCompliant = true, RuleName = "PassRule", Severity = RuleSeverity.Critical });

            var engine  = new GovernanceRuleEngine(new[] { passRule.Object });
            var handler = new GetDashboardQueryHandler(mockFetcher.Object, engine, DefaultConfigLoader().Object);

            // Act
            var result = await handler.HandleAsync(new GetDashboardQuery("o", "r"));

            // Assert
            var row = Assert.Single(result.Agents);
            Assert.Equal("APPROVED", row.Status);
            Assert.Equal(0, row.CriticalViolations);
        }

        // ── Test 4: BLOCKED agent has score < 40 ─────────────────────────────

        /// <summary>
        /// An agent with 4 critical violations gets score ≤ 0 (well below 40) and is BLOCKED.
        /// Score = 100 − (4 × 25) = 0.
        /// </summary>
        [Fact]
        public async Task HandleAsync_BlockedAgent_HasScoreLessThan40()
        {
            // Arrange
            var badAgent = BuildAgent("VeryBadAgent");

            var mockFetcher = new Mock<IAgentFetcher>();
            mockFetcher
                .Setup(f => f.FetchAgentsAsync("o", "r"))
                .ReturnsAsync(new List<AgentDefinition> { badAgent });

            // 4 critical rules all failing → score = 100 - 4*25 = 0
            var criticalRules = Enumerable.Range(1, 4).Select(i =>
            {
                var m = new Mock<IGovernanceRule>();
                m.SetupGet(r => r.RuleName).Returns($"CritRule{i}");
                m.SetupGet(r => r.Severity).Returns(RuleSeverity.Critical);
                m.Setup(r => r.EvaluateAsync(It.IsAny<AgentDefinition>()))
                 .ReturnsAsync(new RuleResult
                 {
                     IsCompliant = false,
                     RuleName    = $"CritRule{i}",
                     Severity    = RuleSeverity.Critical,
                     Violations  = new List<string> { $"critical violation {i}" }
                 });
                return m.Object;
            });

            var engine  = new GovernanceRuleEngine(criticalRules);
            var handler = new GetDashboardQueryHandler(mockFetcher.Object, engine, DefaultConfigLoader().Object);

            // Act
            var result = await handler.HandleAsync(new GetDashboardQuery("o", "r"));

            // Assert
            var row = Assert.Single(result.Agents);
            Assert.Equal("BLOCKED", row.Status);
            Assert.True(row.GovernanceScore < 40,
                $"Expected score < 40 but got {row.GovernanceScore}");
        }
    }
}
