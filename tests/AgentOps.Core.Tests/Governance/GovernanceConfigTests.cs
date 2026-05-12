using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.Governance.Rules;
using AgentOps.Core.ValueObjects;
using Xunit;

namespace AgentOps.Core.Tests.Governance
{
    public class GovernanceConfigTests
    {
        // ── Test 1: Default config has expected values ────────────────────────

        [Fact]
        public void Default_Config_Has_Expected_DefaultAllowedActions()
        {
            var config = GovernanceConfig.Default;

            Assert.NotNull(config.AllowedActions);
            Assert.Contains("read_code",     config.AllowedActions);
            Assert.Contains("post_comment",  config.AllowedActions);
            Assert.Contains("create_report", config.AllowedActions);
        }

        [Fact]
        public void Default_Config_Has_Expected_DefaultForbiddenActions()
        {
            var config = GovernanceConfig.Default;

            Assert.NotNull(config.ForbiddenActions);
            Assert.Contains("push_to_main",    config.ForbiddenActions);
            Assert.Contains("delete_database", config.ForbiddenActions);
            Assert.Contains("execute_code",    config.ForbiddenActions);
        }

        [Fact]
        public void Default_Config_ScoringThresholds_AreCorrect()
        {
            var scoring = GovernanceConfig.Default.Scoring;

            Assert.Equal(25, scoring.CriticalPenalty);
            Assert.Equal(10, scoring.WarningPenalty);
            Assert.Equal(40, scoring.BlockedThreshold);
            Assert.Equal(70, scoring.ReviewThreshold);
        }

        // ── Test 2: Custom config overrides defaults ──────────────────────────

        [Fact]
        public void Custom_Config_Overrides_AllowedActions()
        {
            var custom = new GovernanceConfig
            {
                AllowedActions = new List<string> { "read_code", "deploy_to_staging" }
            };

            Assert.Equal(2, custom.AllowedActions.Count);
            Assert.Contains("deploy_to_staging", custom.AllowedActions);
        }

        [Fact]
        public void Custom_Config_Overrides_ScoringPenalties()
        {
            var custom = new GovernanceConfig
            {
                Scoring = new ScoringConfig
                {
                    CriticalPenalty  = 50,
                    WarningPenalty   = 20,
                    BlockedThreshold = 60,
                    ReviewThreshold  = 80
                }
            };

            Assert.Equal(50, custom.Scoring.CriticalPenalty);
            Assert.Equal(20, custom.Scoring.WarningPenalty);
            Assert.Equal(60, custom.Scoring.BlockedThreshold);
            Assert.Equal(80, custom.Scoring.ReviewThreshold);
        }

        // ── Test 3: Allowed actions custom work in AllowedActionsRule ─────────

        [Fact]
        public async Task AllowedActionsRule_CustomConfig_PermitsExtraAction()
        {
            // Config that adds "deploy_to_staging" to the allowed set
            var config = new GovernanceConfig
            {
                AllowedActions = new List<string>
                {
                    "read_code", "post_comment", "deploy_to_staging"
                }
            };

            var agent = BuildAgent(new[] { "read_code", "deploy_to_staging" });
            var rule  = new AllowedActionsRule();

            // cast to IConfigurableGovernanceRule
            var configRule = (IConfigurableGovernanceRule)rule;
            var result = await configRule.EvaluateAsync(agent, config);

            Assert.True(result.IsCompliant,
                "deploy_to_staging should be allowed when it is in the custom config");
        }

        [Fact]
        public async Task AllowedActionsRule_DefaultConfig_BlocksNonDefaultAction()
        {
            var agent = BuildAgent(new[] { "read_code", "deploy_to_staging" });
            var rule  = new AllowedActionsRule();

            // Default config does not include deploy_to_staging
            var result = await rule.EvaluateAsync(agent);

            Assert.False(result.IsCompliant);
            Assert.Contains(result.Violations, v => v.Contains("deploy_to_staging"));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static AgentDefinition BuildAgent(string[] actions)
        {
            var config = new AgentConfiguration { Owner = "owner" };
            foreach (var a in actions) config.AllowedActions.Add(a);

            return new AgentDefinition(
                new AgentId("test-id"),
                "Test Agent",
                "Test agent description for governance config tests",
                purpose: "Testing",
                rules: new List<string> { "rule" },
                tools: new List<string> { "tool" },
                configuration: config,
                createdAt: System.DateTime.UtcNow,
                version: "1.0.0");
        }
    }
}
