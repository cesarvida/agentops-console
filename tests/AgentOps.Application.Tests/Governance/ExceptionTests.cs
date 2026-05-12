using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.Governance;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.Governance.Rules;
using AgentOps.Core.ValueObjects;
using Moq;
using Xunit;

namespace AgentOps.Application.Tests.Governance
{
    public class ExceptionTests
    {
        // A rule that always returns a Critical violation with a fixed name
        private static readonly string TestRuleName = "Allowed Actions Whitelist";

        private static GovernanceRuleEngine BuildEngine()
        {
            // Use the real AllowedActionsRule so we get a Critical violation
            // when the agent declares an invalid action
            return new GovernanceRuleEngine(new IGovernanceRule[]
            {
                new AllowedActionsRule()
            });
        }

        // ── Test 1: Active exception downgrades Critical to Warning ───────────

        [Fact]
        public async Task ActiveException_Downgrades_Critical_To_Warning()
        {
            // Agent declares a forbidden action → AllowedActionsRule fires Critical
            var agent = BuildAgentWithAction("hack_everything",
                exceptions: new[]
                {
                    new GovernanceException
                    {
                        RuleName   = TestRuleName,
                        Reason     = "Temporary need",
                        ApprovedBy = "cto@company.com",
                        ExpiresAt  = DateTime.UtcNow.AddDays(30)   // active
                    }
                });

            var engine = BuildEngine();
            var report = await engine.EvaluateAsync(agent);

            // Critical violation should be downgraded → no Criticals
            Assert.Equal(0, report.CriticalViolations);
            // Should still show as a Warning
            Assert.True(report.WarningViolations > 0);
            Assert.True(report.HasExceptionOverrides);

            var downgraded = report.RuleResults.First(r => r.RuleName == TestRuleName);
            Assert.Equal(RuleSeverity.Warning, downgraded.Severity);
            Assert.False(string.IsNullOrEmpty(downgraded.ExceptionNote));
            Assert.Contains("cesarvida".Length > 0 ? "cto@company.com" : "", downgraded.ExceptionNote!);
        }

        // ── Test 2: Expired exception does NOT apply ──────────────────────────

        [Fact]
        public async Task ExpiredException_Does_Not_Apply_Rule_Stays_Critical()
        {
            var agent = BuildAgentWithAction("hack_everything",
                exceptions: new[]
                {
                    new GovernanceException
                    {
                        RuleName   = TestRuleName,
                        Reason     = "Expired exception",
                        ApprovedBy = "cto@company.com",
                        ExpiresAt  = DateTime.UtcNow.AddDays(-1)   // expired yesterday
                    }
                });

            var engine = BuildEngine();
            var report = await engine.EvaluateAsync(agent);

            Assert.Equal(1, report.CriticalViolations);
            Assert.False(report.HasExceptionOverrides);
            Assert.Equal("BLOCKED", report.FinalStatus);
        }

        // ── Test 3: Exception without ApprovedBy is not valid ─────────────────

        [Fact]
        public async Task ExceptionWithoutApprovedBy_IsNot_Valid()
        {
            var agent = BuildAgentWithAction("hack_everything",
                exceptions: new[]
                {
                    new GovernanceException
                    {
                        RuleName   = TestRuleName,
                        Reason     = "No approver",
                        ApprovedBy = "",   // ← empty: invalid
                        ExpiresAt  = DateTime.UtcNow.AddDays(30)
                    }
                });

            var engine = BuildEngine();
            var report = await engine.EvaluateAsync(agent);

            // Without approver the exception is ignored → stays Critical
            Assert.Equal(1, report.CriticalViolations);
            Assert.False(report.HasExceptionOverrides);
        }

        // ── Test 4: Score is higher with active exception than without ────────

        [Fact]
        public async Task Score_Is_Higher_With_Active_Exception_Than_Without()
        {
            var engine = BuildEngine();

            // Without exception
            var agentNoEx = BuildAgentWithAction("hack_everything");
            var reportNoEx = await engine.EvaluateAsync(agentNoEx);

            // With active exception (Critical → Warning)
            var agentWithEx = BuildAgentWithAction("hack_everything",
                exceptions: new[]
                {
                    new GovernanceException
                    {
                        RuleName   = TestRuleName,
                        Reason     = "Approved temp",
                        ApprovedBy = "admin",
                        ExpiresAt  = DateTime.UtcNow.AddDays(14)
                    }
                });
            var reportWithEx = await engine.EvaluateAsync(agentWithEx);

            Assert.True(reportWithEx.GovernanceScore > reportNoEx.GovernanceScore,
                $"Expected score with exception ({reportWithEx.GovernanceScore}) > score without ({reportNoEx.GovernanceScore})");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static AgentDefinition BuildAgentWithAction(
            string action,
            GovernanceException[]? exceptions = null)
        {
            var config = new AgentConfiguration { Owner = "team" };
            config.AllowedActions.Add(action);

            return new AgentDefinition(
                new AgentId(Guid.NewGuid().ToString()),
                "Exception Test Agent",
                "Agent used to test governance exception handling",
                purpose: "Exception testing",
                rules: new List<string> { "rule" },
                tools: new List<string> { "tool" },
                configuration: config,
                createdAt: DateTime.UtcNow,
                version: "1.0.0")
            {
                Exceptions = exceptions != null
                    ? new List<GovernanceException>(exceptions)
                    : new List<GovernanceException>()
            };
        }
    }
}
