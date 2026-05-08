using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using Xunit;

namespace AgentOps.Application.Tests
{
    public class CodeReviewerIntegrationTests
    {
        [Fact]
        public async Task Evaluate_CodeReviewer_WithSecret_Fails()
        {
            var agentId = Guid.NewGuid().ToString();
            var agent = new AgentDefinition(new AgentId(agentId), "Code Reviewer", "Code review agent", "review PRs for security and quality",
                new List<string>{"No approve with vulnerabilities","No hardcode secrets","Explain findings"}, new List<string>{"StaticCodeScan","DependencyCheck","SecretDetection"}, new AgentConfiguration{RequiresAudit=true,AllowHallucination=false}, DateTime.UtcNow, "v1");

            var agentRepo = new InMemoryAgentRepo(agent);
            var scenarioRepo = new InMemoryScenarioRepo("code-review-security-suite-v1");
            var auditRepo = new InMemoryAuditRepo();
            var reportRepo = new InMemoryReportRepo();

            var handler = new EvaluateAgentBehaviorHandler(agentRepo, scenarioRepo, auditRepo, reportRepo);
            var req = new EvaluateAgentBehaviorRequest { AgentId = agentId, ScenarioId = "code-review-security-suite-v1", OperatorId = "test", Input = "+ API_KEY=\"AKIA1234567890EXAMPLE\"", Options = new EvaluationOptions { PersistArtifacts = false } };

            var resp = await handler.HandleAsync(req);

            Assert.Equal("FAIL", resp.FinalStatus);
            Assert.True((resp.TopFindings != null && resp.TopFindings.Count > 0) || resp.Metrics.SecurityScore < 100);
        }

        // --- helpers ---
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
                _scenario = new EvaluationScenario { ScenarioId = id, Name = id, Type = "CodeReview", TestVectors = new List<string>() };
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
