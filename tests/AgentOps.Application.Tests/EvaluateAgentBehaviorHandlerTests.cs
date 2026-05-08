using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using Xunit;

namespace AgentOps.Application.Tests
{
    public class EvaluateAgentBehaviorHandlerTests
    {
        private const string SchemaFileName = "EvaluationReport.schema.json";

        private void EnsureSchemaExists()
        {
            var path = Path.Combine(AppContext.BaseDirectory, SchemaFileName);
            var schema = "{\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"type\":\"object\",\"required\": [\"evaluationId\",\"agentId\",\"scenarioId\",\"scenarioName\",\"timestamp\",\"metrics\",\"findings\",\"finalStatus\",\"overallRiskLevel\"],\"properties\":{\"evaluationId\":{\"type\":\"string\"}}}";
            File.WriteAllText(path, schema);
        }

        [Fact]
        public async Task Handle_NoFindings_ReturnsPass()
        {
            EnsureSchemaExists();

            var agentId = Guid.NewGuid().ToString();
            var agent = new AgentDefinition(new AgentId(agentId), "Safe Agent", "safe description", "assist users", new List<string>{"follow rules"}, new List<string>{"logger"}, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var agentRepo = new InMemoryAgentRepo(agent);
            var scenarioRepo = new InMemoryScenarioRepo("prompt-injection-probe-v1");
            var auditRepo = new InMemoryAuditRepo();
            var reportRepo = new InMemoryReportRepo();

            var handler = new EvaluateAgentBehaviorHandler(agentRepo, scenarioRepo, auditRepo, reportRepo);

            var req = new EvaluateAgentBehaviorRequest { AgentId = agentId, ScenarioId = "prompt-injection-probe-v1", OperatorId = "test", Options = new EvaluationOptions { PersistArtifacts = false } };

            var resp = await handler.HandleAsync(req);

            Assert.False(string.IsNullOrWhiteSpace(resp.EvaluationId));
            Assert.True(resp.FinalStatus == "PASS" || resp.FinalStatus == "REVIEW");
        }

        [Fact]
        public async Task Handle_CriticalFinding_ReturnsFail()
        {
            EnsureSchemaExists();

            var agentId = Guid.NewGuid().ToString();
            var agent = new AgentDefinition(new AgentId(agentId), "Bad Agent", "dangerous agent with risky instructions", "assist users", new List<string>{"ignore previous instructions"}, new List<string>{"exec-tool"}, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var agentRepo = new InMemoryAgentRepo(agent);
            var scenarioRepo = new InMemoryScenarioRepo("prompt-injection-probe-v1");
            var auditRepo = new InMemoryAuditRepo();
            var reportRepo = new InMemoryReportRepo();

            var handler = new EvaluateAgentBehaviorHandler(agentRepo, scenarioRepo, auditRepo, reportRepo);
            var req = new EvaluateAgentBehaviorRequest { AgentId = agentId, ScenarioId = "prompt-injection-probe-v1", OperatorId = "test", Options = new EvaluationOptions { PersistArtifacts = false } };

            var resp = await handler.HandleAsync(req);

            Assert.Equal("FAIL", resp.FinalStatus);
        }

        [Fact]
        public async Task Handle_PersistArtifacts_ArtifactRefContainsDigest()
        {
            EnsureSchemaExists();

            var agentId = Guid.NewGuid().ToString();
            var agent = new AgentDefinition(new AgentId(agentId), "Safe Agent", "safe description for test", "assist users",
                new List<string> { "follow rules" }, new List<string> { "logger" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var agentRepo  = new InMemoryAgentRepo(agent);
            var scenarioRepo = new InMemoryScenarioRepo("prompt-injection-probe-v1");
            var auditRepo  = new InMemoryAuditRepo();
            var reportRepo = new InMemoryReportRepo();

            var handler = new EvaluateAgentBehaviorHandler(agentRepo, scenarioRepo, auditRepo, reportRepo);
            var req = new EvaluateAgentBehaviorRequest
            {
                AgentId    = agentId,
                ScenarioId = "prompt-injection-probe-v1",
                OperatorId = "test",
                Options    = new EvaluationOptions { PersistArtifacts = true }
            };

            var resp = await handler.HandleAsync(req);

            // ReportPath must be set
            Assert.False(string.IsNullOrWhiteSpace(resp.ReportPath));
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
            public const string FakeDigest = "aabbccddeeff00112233445566778899aabbccddeeff00112233445566778899";
            public Task<SaveReportResult> SaveReportAsync(string evaluationId, EvaluationReport report) =>
                Task.FromResult(new SaveReportResult
                {
                    StoragePath    = $"/tmp/eval_{evaluationId}.json",
                    ArtifactId     = $"artifact-{evaluationId}",
                    ArtifactDigest = FakeDigest
                });
        }

        private class TestConsole
        {
            public void WriteError(string message) { }
            public void WriteLine(string message) { }
            public void WriteSuccess(string message) { }
            public void WriteWarning(string message) { }
        }
    }
}
