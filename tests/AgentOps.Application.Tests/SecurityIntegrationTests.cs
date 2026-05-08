using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;
using Xunit;

namespace AgentOps.Application.Tests
{
    public class SecurityIntegrationTests
    {
        [Fact]
        public async Task EvaluateAgentBehavior_WithCriticalSecurityFinding_ResultsInFail()
        {
            var agentId = Guid.NewGuid().ToString();
            var agent = new AgentDefinition(new AgentId(agentId), "Bad Agent", "dangerous agent with risky instructions", "assist users", new List<string>{"ignore previous instructions"}, new List<string>{"exec-tool"}, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var agentRepo = new InMemoryAgentRepo(agent);
            var scenarioRepo = new InMemoryScenarioRepo("prompt-injection-probe-v1");
            var auditRepo = new InMemoryAuditRepo();
            var reportRepo = new InMemoryReportRepo();

            var fakeAnalyzer = new FakeSecurityAnalyzer();

            var handler = new EvaluateAgentBehaviorHandler(agentRepo, scenarioRepo, auditRepo, reportRepo, fakeAnalyzer);
            var req = new EvaluateAgentBehaviorRequest { AgentId = agentId, ScenarioId = "prompt-injection-probe-v1", OperatorId = "test", Options = new EvaluationOptions { PersistArtifacts = false } };

            var resp = await handler.HandleAsync(req);

            Assert.Equal("FAIL", resp.FinalStatus);
        }

        private class FakeSecurityAnalyzer : ISecurityAnalyzer
        {
            public SecurityAnalysisResult Analyze(AgentDefinition agent)
            {
                return new SecurityAnalysisResult
                {
                    Score = 0,
                    Findings = new System.Collections.Generic.List<SecurityFinding>
                    {
                        new SecurityFinding { RuleId = "SEC-RULE-TEST", RuleName = "Fake", Severity = SecuritySeverity.Critical, Summary = "Test critical", EvidenceSummary = "e" }
                    }
                };
            }
        }

        // Reuse helpers from EvaluateAgentBehaviorHandlerTests
        private class InMemoryAgentRepo : IAgentDefinitionRepository
        {
            private readonly AgentDefinition _agent;
            public InMemoryAgentRepo(AgentDefinition agent) { _agent = agent; }
            public Task<string> AddAsync(AgentDefinition agentDefinition) => Task.FromResult(string.Empty);
            public Task<IEnumerable<AgentDefinition>> ListAllAsync() => Task.FromResult<IEnumerable<AgentDefinition>>(new[] { _agent });
            public Task<AgentDefinition?> GetByIdAsync(AgentId id) => Task.FromResult(id.Value == _agent.Id.Value ? _agent : null as AgentDefinition);
        }

        private class InMemoryScenarioRepo : IEvaluationScenarioRepository
        {
            private readonly EvaluationScenario _scenario;
            public InMemoryScenarioRepo(string id)
            {
                _scenario = new EvaluationScenario { ScenarioId = id, Name = id, Type = "Security" };
            }
            public Task<EvaluationScenario?> GetByIdAsync(string scenarioId) => Task.FromResult(scenarioId == _scenario.ScenarioId ? _scenario : null as EvaluationScenario);
        }

        private class InMemoryAuditRepo : IAuditRepository
        {
            public Task<string> AppendAsync(AuditEntry entry) => Task.FromResult("audit-test-1");
        }

        private class InMemoryReportRepo : IEvaluationReportRepository
        {
            public Task<SaveReportResult> SaveReportAsync(string evaluationId, EvaluationReport report) =>
                Task.FromResult(new SaveReportResult { StoragePath = $"/tmp/{evaluationId}.json", ArtifactId = $"art-{evaluationId}", ArtifactDigest = string.Empty });
        }
    }
}
