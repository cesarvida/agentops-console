using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using AgentOps.Security;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Rules;
using Xunit;

namespace AgentOps.Application.Tests
{
    public class ComplianceIntegrationTests
    {
        [Fact]
        public async Task EvaluateAgentBehavior_ComplianceCritical_ResultsInFail()
        {
            var agentId = Guid.NewGuid().ToString();
            // Agent with no compliance statements
            var agent = new AgentDefinition(new AgentId(agentId), "NoCompliance", "simple agent with no compliance statements", "assist users", new List<string>{ "do work" }, new List<string>{ "logger" }, new AgentConfiguration(), DateTime.UtcNow, "v1");

            var agentRepo = new InMemoryAgentRepo(agent);
            var scenarioRepo = new InMemoryScenarioRepo("compliance-checker-suite-v1");
            var auditRepo = new InMemoryAuditRepo();
            var reportRepo = new InMemoryReportRepo();

            // Instantiate SecurityAnalyzer with compliance rules
            var rules = new ISecurityRule[] { new NoComplianceRulesRule(), new MissingRetentionPolicyRule(), new MissingLawfulBasisRule(), new UnclassifiedDataRule(), new MissingJustificationRule() };
            var securityAnalyzer = new SecurityAnalyzer(rules);

            var handler = new EvaluateAgentBehaviorHandler(agentRepo, scenarioRepo, auditRepo, reportRepo, securityAnalyzer);
            var req = new EvaluateAgentBehaviorRequest { AgentId = agentId, ScenarioId = "compliance-checker-suite-v1", OperatorId = "test", Options = new EvaluationOptions { PersistArtifacts = false } };

            var resp = await handler.HandleAsync(req);

            Assert.Equal("FAIL", resp.FinalStatus);
        }

        private class InMemoryAgentRepo : IAgentDefinitionRepository
        {
            private readonly AgentDefinition _agent;
            public InMemoryAgentRepo(AgentDefinition agent) { _agent = agent; }
            public Task<string> AddAsync(AgentDefinition agentDefinition) => Task.FromResult(string.Empty);
            public Task<IEnumerable<AgentDefinition>> ListAllAsync() => Task.FromResult<IEnumerable<AgentDefinition>>(new[] { _agent });
            public Task<AgentDefinition?> GetByIdAsync(AgentId id) => Task.FromResult(id.Value == _agent.Id.Value ? _agent : null as AgentDefinition);
        }

        private class InMemoryScenarioRepo : AgentOps.Application.UseCases.EvaluateAgentBehavior.IEvaluationScenarioRepository
        {
            private readonly AgentOps.Application.UseCases.EvaluateAgentBehavior.Models.EvaluationScenario _scenario;
            public InMemoryScenarioRepo(string id)
            {
                _scenario = new AgentOps.Application.UseCases.EvaluateAgentBehavior.Models.EvaluationScenario { ScenarioId = id, Name = id, Type = "Compliance" };
            }
            public Task<AgentOps.Application.UseCases.EvaluateAgentBehavior.Models.EvaluationScenario?> GetByIdAsync(string scenarioId) => Task.FromResult(scenarioId == _scenario.ScenarioId ? _scenario : null as AgentOps.Application.UseCases.EvaluateAgentBehavior.Models.EvaluationScenario);
        }

        private class InMemoryAuditRepo : AgentOps.Application.UseCases.CreateAgentDefinition.IAuditRepository
        {
            public Task<string> AppendAsync(AgentOps.Application.UseCases.CreateAgentDefinition.AuditEntry entry) => Task.FromResult("audit-test-1");
        }

        private class InMemoryReportRepo : AgentOps.Application.UseCases.EvaluateAgentBehavior.IEvaluationReportRepository
        {
            public Task<AgentOps.Application.UseCases.EvaluateAgentBehavior.SaveReportResult> SaveReportAsync(string evaluationId, AgentOps.Application.UseCases.EvaluateAgentBehavior.Models.EvaluationReport report) =>
                Task.FromResult(new AgentOps.Application.UseCases.EvaluateAgentBehavior.SaveReportResult { StoragePath = $"/tmp/{evaluationId}.json", ArtifactId = $"art-{evaluationId}", ArtifactDigest = string.Empty });
        }
    }
}
