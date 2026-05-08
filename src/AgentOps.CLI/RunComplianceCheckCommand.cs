using System;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.CreateAgentDefinition;

namespace AgentOps.CLI
{
    public class RunComplianceCheckCommand
    {
        private readonly EvaluateAgentBehaviorHandler _handler;
        private readonly IConsoleWriter _console;
        private readonly IAgentDefinitionRepository _agentRepo;

        public RunComplianceCheckCommand(EvaluateAgentBehaviorHandler handler, IConsoleWriter console, IAgentDefinitionRepository agentRepo)
        {
            _handler = handler;
            _console = console;
            _agentRepo = agentRepo;
        }

        public async Task ExecuteAsync()
        {
            _console.WriteLine("=== Run Compliance Check (simulated) ===");
            var agents = await _agentRepo.ListAllAsync();
            var compliance = agents.FirstOrDefault(a => a.Name == "Compliance Checker");
            if (compliance == null)
            {
                _console.WriteWarning("Agent 'Compliance Checker' not found. Please create it first.");
                return;
            }

            _console.WriteLine($"Selected agent: {compliance.Name} (Id: {compliance.Id.Value})");
            _console.WriteLine("Use sample definition check? (yes/no) [yes]: ");
            var useSample = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
            string input = string.Empty;
            if (string.IsNullOrWhiteSpace(useSample) || useSample == "yes")
            {
                input = "agent.definition.sample: compliance-check";
            }
            else
            {
                _console.WriteLine("Paste a short definition excerpt: ");
                input = Console.ReadLine() ?? string.Empty;
            }

            var req = new EvaluateAgentBehaviorRequest
            {
                AgentId = compliance.Id.Value,
                ScenarioId = "compliance-checker-suite-v1",
                OperatorId = "cli",
                Input = input,
                Options = new EvaluationOptions { PersistArtifacts = false, AnonymizeEvidence = true }
            };

            var resp = await _handler.HandleAsync(req);

            _console.WriteLine($"Result: {resp.FinalStatus} - Risk: {resp.OverallRiskLevel}");
            _console.WriteLine($"Scores: Compliance {resp.Metrics.ComplianceScore} / Security {resp.Metrics.SecurityScore} / Consistency {resp.Metrics.ConsistencyScore}");
            if (resp.TopFindings != null && resp.TopFindings.Count > 0)
            {
                _console.WriteLine("Top findings:");
                foreach (var f in resp.TopFindings)
                {
                    _console.WriteLine($"- [{f.Severity}] {f.Summary} -> {f.Category}");
                }
            }
        }
    }
}
