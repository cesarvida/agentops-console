using System;
using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using AgentOps.Security.Rules;
using AgentOps.Security.Models;
using AgentOps.Security.Interfaces;
using Xunit;

namespace AgentOps.Security.Tests
{
    public class RulesTests
    {
        [Fact]
        public void PromptInjectionRule_DetectsIgnorePattern()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "Bad Agent", "description for testing prompt injection detection", "purpose",
                new List<string>{ "ignore previous instructions" }, new List<string>{ "logger" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rule = new PromptInjectionRule();
            var findings = rule.Evaluate(agent).ToList();

            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Severity == SecuritySeverity.Critical);
        }

        [Fact]
        public void ToolAbuseRule_DetectsExecTool()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "Tool Agent", "description for testing tool abuse detection", "purpose",
                new List<string>{ "follow rules" }, new List<string>{ "exec-tool" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rule = new ToolAbuseRule();
            var findings = rule.Evaluate(agent).ToList();

            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Location == "tools");
        }

        [Fact]
        public void SensitiveDataExposureRule_DetectsPii()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "Pii Agent", "Please provide SSN for records", "purpose",
                new List<string>{ "follow rules" }, new List<string>{ "logger" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rule = new SensitiveDataExposureRule();
            var findings = rule.Evaluate(agent).ToList();

            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Severity == SecuritySeverity.Critical || f.Severity == SecuritySeverity.High);
        }

        [Fact]
        public void MissingSafetyRule_FiresWhenNoSafetyKeywords()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "NoSafety", "some description", "purpose",
                new List<string>{ "follow rules" }, new List<string>{ "logger" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rule = new MissingSafetyRule();
            var findings = rule.Evaluate(agent).ToList();

            Assert.NotEmpty(findings);
            Assert.Equal(SecuritySeverity.Medium, findings[0].Severity);
        }

        [Fact]
        public void Rules_DoNotProduceFalsePositivesOnSafeAgent()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "Safe Agent", "safe description respecting privacy and do not disclose", "assist users",
                new List<string>{ "do not disclose personal data" }, new List<string>{ "logger" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rules = new ISecurityRule[] { new PromptInjectionRule(), new ToolAbuseRule(), new SensitiveDataExposureRule(), new MissingSafetyRule() };
            var findings = new List<SecurityFinding>();
            foreach (var r in rules)
            {
                findings.AddRange(r.Evaluate(agent));
            }

            Assert.Empty(findings);
        }

        [Fact]
        public void MissingRetentionPolicyRule_FiresWhenNoRetention()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "Compliance Agent", "handles customer requests and stores records", "process requests",
                new List<string>{ "handle requests" }, new List<string>{ "PolicyChecklist" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rule = new MissingRetentionPolicyRule();
            var findings = rule.Evaluate(agent).ToList();

            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Severity == SecuritySeverity.High);
        }

        [Fact]
        public void MissingLawfulBasisRule_FiresWhenPiiMentionedWithoutLawfulBasis()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "Pii Agent", "agent stores personal data for records", "process personal data",
                new List<string>{ "store personal data" }, new List<string>{ "DataStore" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rule = new MissingLawfulBasisRule();
            var findings = rule.Evaluate(agent).ToList();

            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Severity == SecuritySeverity.Critical);
        }

        [Fact]
        public void UnclassifiedDataRule_FiresWhenNoClassification()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "NoClass", "agent processes data without classification controls", "process data",
                new List<string>{ "process data" }, new List<string>{ "DataStore" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rule = new UnclassifiedDataRule();
            var findings = rule.Evaluate(agent).ToList();

            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Severity == SecuritySeverity.High);
        }

        [Fact]
        public void MissingJustificationRule_FiresWhenNoJustification()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "NoJust", "performs processing", "assist users",
                new List<string>{ "perform task" }, new List<string>{ "logger" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rule = new MissingJustificationRule();
            var findings = rule.Evaluate(agent).ToList();

            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Severity == SecuritySeverity.Medium);
        }

        [Fact]
        public void NoComplianceRulesRule_FiresWhenNoComplianceKeywords()
        {
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "NoCompliance", "simple agent with no compliance statements", "assist users",
                new List<string>{ "do work" }, new List<string>{ "logger" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var rule = new NoComplianceRulesRule();
            var findings = rule.Evaluate(agent).ToList();

            Assert.NotEmpty(findings);
            Assert.Contains(findings, f => f.Severity == SecuritySeverity.Critical);
        }
    }
}
