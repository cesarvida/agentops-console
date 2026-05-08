using System;
using System.Collections.Generic;
using System.Linq;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using Xunit;

namespace AgentOps.Application.Tests
{
    public class CodeReviewerAnalyzersTests
    {
        [Fact]
        public void SecretPatternAnalyzer_FindsApiKey()
        {
            var analyzer = new SecretPatternAnalyzer();
            var scenario = new EvaluationScenario { TestVectors = new List<string> { "+ API_KEY=\"AKIA1234567890EXAMPLE\"" } };
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "Code Reviewer", "deterministic code reviewer for tests", "review code", new List<string>{"do not disclose"}, new List<string>{"StaticCodeScan"}, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var res = analyzer.Analyze(agent, scenario);
            Assert.NotEmpty(res.Findings);
            Assert.Contains(res.Findings, f => f.Severity == "Critical");
        }

        [Fact]
        public void DangerousFunctionAnalyzer_FindsEval()
        {
            var analyser = new DangerousFunctionAnalyzer();
            var scenario = new EvaluationScenario { TestVectors = new List<string> { "+ eval(user_input)" } };
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "Code Reviewer", "deterministic code reviewer for tests", "review code", new List<string>{"do not disclose"}, new List<string>{"StaticCodeScan"}, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var res = analyser.Analyze(agent, scenario);
            Assert.NotEmpty(res.Findings);
            Assert.Contains(res.Findings, f => f.Summary.Contains("eval") || f.EvidenceSummary.Contains("eval"));
        }

        [Fact]
        public void DependencyRiskAnalyzer_FindsKnownVuln()
        {
            var analyser = new DependencyRiskAnalyzer();
            var scenario = new EvaluationScenario { TestVectors = new List<string> { "+ vulnerable-lib==1.0.0" } };
            var agent = new AgentDefinition(new AgentId(Guid.NewGuid().ToString()), "Code Reviewer", "deterministic code reviewer for tests", "review code", new List<string>{"do not disclose"}, new List<string>{"DependencyCheck"}, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var res = analyser.Analyze(agent, scenario);
            Assert.NotEmpty(res.Findings);
            Assert.Contains(res.Findings, f => f.EvidenceSummary.Contains("vulnerable-lib"));
        }
    }
}
