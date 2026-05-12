using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.Governance.Rules;
using AgentOps.Core.ValueObjects;
using AgentOps.Application.Governance;

namespace AgentOps.Application.Tests.Governance
{
    public class GovernanceRuleEngineTests
    {
        private static readonly List<IGovernanceRule> AllRules = new()
        {
            new AllowedActionsRule(),
            new ForbiddenActionsRule(),
            new AuditLoggingRule(),
            new OwnerDefinedRule(),
            new VersionDefinedRule()
        };

        private static AgentDefinition BuildAgent(AgentConfiguration config, string version = "1.0.0", string id = "test-agent")
        {
            return new AgentDefinition(
                new AgentId(id),
                "Test Agent Name",
                "Test agent description used in unit tests",
                purpose: "Testing governance rules",
                rules: new List<string> { "enforce_security" },
                tools: new List<string> { "code_analysis" },
                configuration: config,
                createdAt: DateTime.UtcNow,
                version: version
            );
        }

        /// <summary>
        /// Test 1: Agent with all allowed actions should pass with APPROVED status
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_CompliantAgent_ReturnsApprovedStatus()
        {
            // Arrange
            var engine = new GovernanceRuleEngine(AllRules);

            var config = new AgentConfiguration
            {
                RequiresAudit = true,
                Owner = "team-backend",
                AllowedActions = new List<string> { "read_code", "post_comment", "create_report" }
            };

            var agent = BuildAgent(config, version: "1.0.0", id: "compliant-agent");

            // Act
            var report = await engine.EvaluateAsync(agent);

            // Assert
            Assert.True(report.IsCompliant);
            Assert.Equal("APPROVED", report.FinalStatus);
            Assert.Equal(100, report.GovernanceScore);
            Assert.Equal(0, report.CriticalViolations);
            Assert.Equal(0, report.WarningViolations);
        }

        /// <summary>
        /// Test 2: Agent with forbidden action should be BLOCKED
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_AgentWithForbiddenAction_ReturnsBlockedStatus()
        {
            // Arrange
            var engine = new GovernanceRuleEngine(AllRules);

            var config = new AgentConfiguration
            {
                RequiresAudit = true,
                Owner = "team-backend",
                AllowedActions = new List<string> { "read_code", "push_to_main" } // push_to_main is forbidden!
            };

            var agent = BuildAgent(config, version: "1.0.0", id: "bad-agent");

            // Act
            var report = await engine.EvaluateAsync(agent);

            // Assert
            Assert.False(report.IsCompliant);
            Assert.Equal("BLOCKED", report.FinalStatus);
            Assert.True(report.GovernanceScore < 100);
            Assert.Equal(2, report.CriticalViolations); // AllowedActionsRule + ForbiddenActionsRule both triggered
        }

        /// <summary>
        /// Test 3: Agent without audit logging should be BLOCKED
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_AgentWithoutAuditLogging_ReturnsBlockedStatus()
        {
            // Arrange
            var engine = new GovernanceRuleEngine(AllRules);

            var config = new AgentConfiguration
            {
                RequiresAudit = false, // Audit disabled!
                Owner = "team-backend",
                AllowedActions = new List<string> { "read_code" }
            };

            var agent = BuildAgent(config, version: "1.0.0", id: "no-audit-agent");

            // Act
            var report = await engine.EvaluateAsync(agent);

            // Assert
            Assert.False(report.IsCompliant);
            Assert.Equal("BLOCKED", report.FinalStatus);
            Assert.Equal(1, report.CriticalViolations);
        }

        /// <summary>
        /// Test 4: Agent without owner should be REVIEW (warning, not critical)
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_AgentWithoutOwner_ReturnsReviewStatus()
        {
            // Arrange
            var engine = new GovernanceRuleEngine(AllRules);

            var config = new AgentConfiguration
            {
                RequiresAudit = true,
                Owner = "", // No owner!
                AllowedActions = new List<string> { "read_code" }
            };

            var agent = BuildAgent(config, version: "1.0.0", id: "no-owner-agent");

            // Act
            var report = await engine.EvaluateAsync(agent);

            // Assert
            Assert.True(report.IsCompliant); // Compliant because only warnings, no critical violations
            Assert.Equal("APPROVED", report.FinalStatus); // Score 90 >= 70, so APPROVED
            Assert.Equal(0, report.CriticalViolations);
            Assert.Equal(1, report.WarningViolations);
            Assert.Equal(90, report.GovernanceScore); // 100 - 10 for warning
        }

        /// <summary>
        /// Test 5: Agent with mixed violations should calculate score correctly
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_AgentWithMixedViolations_CalculatesScoreCorrectly()
        {
            // Arrange
            var engine = new GovernanceRuleEngine(AllRules);

            var config = new AgentConfiguration
            {
                RequiresAudit = false, // Critical violation
                Owner = "", // Warning violation
                AllowedActions = new List<string> { "read_code" }
            };

            var agent = BuildAgent(config, version: "1.0.0", id: "mixed-violations-agent");

            // Act
            var report = await engine.EvaluateAsync(agent);

            // Assert
            Assert.False(report.IsCompliant); // Has critical violation
            Assert.Equal("BLOCKED", report.FinalStatus);
            Assert.Equal(1, report.CriticalViolations);
            Assert.Equal(1, report.WarningViolations);
            // Score: 100 - (1*25) - (1*10) = 65
            Assert.Equal(65, report.GovernanceScore);
        }

        /// <summary>
        /// Test 6: Agent with invalid version should be REVIEW
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_AgentWithInvalidVersion_ReturnsReviewStatus()
        {
            // Arrange
            var engine = new GovernanceRuleEngine(AllRules);

            var config = new AgentConfiguration
            {
                RequiresAudit = true,
                Owner = "team-backend",
                AllowedActions = new List<string> { "read_code" }
            };

            var agent = BuildAgent(config, version: "latest", id: "bad-version-agent"); // Invalid version!

            // Act
            var report = await engine.EvaluateAsync(agent);

            // Assert
            Assert.True(report.IsCompliant); // Only warnings
            Assert.Equal("APPROVED", report.FinalStatus); // Score 90 >= 70, so APPROVED
            Assert.Equal(0, report.CriticalViolations);
            Assert.Equal(1, report.WarningViolations);
            Assert.Equal(90, report.GovernanceScore);
        }
    }
}
