using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.Governance.Rules;
using AgentOps.Core.ValueObjects;
using Xunit;

namespace AgentOps.Core.Tests.Governance.Rules
{
    public class NewRulesTests
    {
        // ── RateLimitRule ────────────────────────────────────────────────────

        [Fact]
        public async Task RateLimitRule_Pass_With60ReqPerMin()
        {
            var agent  = BuildAgent(rpm: 60);
            var result = await new RateLimitRule().EvaluateAsync(agent);

            Assert.True(result.IsCompliant);
        }

        [Fact]
        public async Task RateLimitRule_Fail_WithoutRateLimit()
        {
            var agent  = BuildAgent(rpm: null);
            var result = await new RateLimitRule().EvaluateAsync(agent);

            Assert.False(result.IsCompliant);
            Assert.Equal(RuleSeverity.Warning, result.Severity);
            Assert.Contains(result.Violations, v => v.Contains("No rate limit"));
        }

        [Fact]
        public async Task RateLimitRule_Fail_WhenRateLimitExceeds1000()
        {
            var agent  = BuildAgent(rpm: 1001);
            var result = await new RateLimitRule().EvaluateAsync(agent);

            Assert.False(result.IsCompliant);
            Assert.Equal(RuleSeverity.Warning, result.Severity);
            Assert.Contains(result.Violations, v => v.Contains("1001") && v.Contains("too high"));
        }

        // ── TimeoutRule ──────────────────────────────────────────────────────

        [Fact]
        public async Task TimeoutRule_Pass_With30Seconds()
        {
            var agent  = BuildAgent(timeout: 30);
            var result = await new TimeoutRule().EvaluateAsync(agent);

            Assert.True(result.IsCompliant);
        }

        [Fact]
        public async Task TimeoutRule_Fail_WhenTimeoutExceeds300()
        {
            var agent  = BuildAgent(timeout: 301);
            var result = await new TimeoutRule().EvaluateAsync(agent);

            Assert.False(result.IsCompliant);
            Assert.Equal(RuleSeverity.Warning, result.Severity);
            Assert.Contains(result.Violations, v => v.Contains("301"));
        }

        [Fact]
        public async Task TimeoutRule_Fail_WhenNoTimeout()
        {
            var agent  = BuildAgent(timeout: null);
            var result = await new TimeoutRule().EvaluateAsync(agent);

            Assert.False(result.IsCompliant);
            Assert.Equal(RuleSeverity.Warning, result.Severity);
            Assert.Contains(result.Violations, v => v.Contains("No timeout"));
        }

        // ── EnvironmentScopeRule ─────────────────────────────────────────────

        [Fact]
        public async Task EnvironmentScopeRule_Pass_WithValidEnvironments()
        {
            var agent  = BuildAgent(environments: new[] { "development", "staging" });
            var result = await new EnvironmentScopeRule().EvaluateAsync(agent);

            Assert.True(result.IsCompliant);
        }

        [Fact]
        public async Task EnvironmentScopeRule_Fail_WithoutEnvironments()
        {
            var agent  = BuildAgent(environments: Array.Empty<string>());
            var result = await new EnvironmentScopeRule().EvaluateAsync(agent);

            Assert.False(result.IsCompliant);
            Assert.Equal(RuleSeverity.Critical, result.Severity);
            Assert.Contains(result.Violations, v => v.Contains("No deployment environments"));
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static AgentDefinition BuildAgent(
            int? rpm = null,
            int? timeout = null,
            string[]? environments = null)
        {
            var config = new AgentConfiguration
            {
                Owner                     = "team",
                RequiresAudit             = true,
                RateLimitRequestsPerMinute = rpm,
                TimeoutSeconds            = timeout
            };

            if (environments != null)
                foreach (var e in environments) config.Environments.Add(e);

            return new AgentDefinition(
                new AgentId(Guid.NewGuid().ToString()),
                "Test Agent Name",
                "Test agent description for new rules tests",
                purpose: "Testing new rules",
                rules: new List<string> { "rule" },
                tools: new List<string> { "tool" },
                configuration: config,
                createdAt: DateTime.UtcNow,
                version: "1.0.0");
        }
    }
}
