using System;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.CreateAgentDefinition;

namespace AgentOps.CLI
{
    public class EvaluateAgentBehaviorCommand
    {
        private readonly EvaluateAgentBehaviorHandler _handler;
        private readonly IConsoleWriter _console;
        private readonly IAgentDefinitionRepository _agentRepo;

        public EvaluateAgentBehaviorCommand(EvaluateAgentBehaviorHandler handler, IConsoleWriter console, IAgentDefinitionRepository agentRepo)
        {
            _handler = handler;
            _console = console;
            _agentRepo = agentRepo;
        }

        public async Task ExecuteAsync()
        {
            _console.WriteLine("=== Evaluate Agent Behavior (MVP static) ===");
            _console.WriteLine("Enter AgentId (or 'list' to pick): ");
            var input = Console.ReadLine() ?? string.Empty;
            string agentId = input;
            if (input.Trim().ToLowerInvariant() == "list")
            {
                var agents = await _agentRepo.ListAllAsync();
                var arr = agents.ToArray();
                for (int i = 0; i < arr.Length; i++)
                {
                    _console.WriteLine($"{i+1}) {arr[i].Name} (Id: {arr[i].Id.Value})");
                }
                _console.WriteLine("Select number: ");
                var sel = Console.ReadLine() ?? string.Empty;
                if (int.TryParse(sel, out var idx) && idx >= 1 && idx <= arr.Length)
                {
                    agentId = arr[idx - 1].Id.Value;
                }
                else
                {
                    _console.WriteWarning("Invalid selection");
                    return;
                }
            }

            _console.WriteLine("Enter scenario id (e.g. prompt-injection-probe-v1): ");
            var scenario = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(scenario)) { _console.WriteWarning("ScenarioId required"); return; }

            _console.WriteLine("Strictness (strict/relaxed) [strict]: ");
            var strict = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(strict)) strict = "strict";

            _console.WriteLine("Persist report? (yes/no) [no]: ");
            var persist = (Console.ReadLine() ?? "").Trim().ToLowerInvariant() == "yes";

            var req = new EvaluateAgentBehaviorRequest
            {
                AgentId = agentId,
                ScenarioId = scenario,
                OperatorId = "cli",
                Options = new EvaluationOptions { Strictness = strict, PersistArtifacts = persist, AnonymizeEvidence = true }
            };

            var resp = await _handler.HandleAsync(req);

            _console.WriteLine($"Result: {resp.FinalStatus} - Risk: {resp.OverallRiskLevel}");
            _console.WriteLine($"Scores: Security {resp.Metrics.SecurityScore} / Compliance {resp.Metrics.ComplianceScore} / Consistency {resp.Metrics.ConsistencyScore}");
            if (resp.TopFindings.Any())
            {
                _console.WriteLine("Top findings:");
                foreach (var f in resp.TopFindings)
                {
                    _console.WriteLine($"- [{f.Severity}] {f.Summary}");
                }
            }
            if (!string.IsNullOrWhiteSpace(resp.ReportPath))
            {
                _console.WriteLine($"Report saved: {resp.ReportPath}");
            }
        }
    }
}
