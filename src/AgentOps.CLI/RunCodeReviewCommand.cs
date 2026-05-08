using System;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.CreateAgentDefinition;

namespace AgentOps.CLI
{
    public class RunCodeReviewCommand
    {
        private readonly EvaluateAgentBehaviorHandler _handler;
        private readonly IConsoleWriter _console;
        private readonly IAgentDefinitionRepository _agentRepo;

        public RunCodeReviewCommand(EvaluateAgentBehaviorHandler handler, IConsoleWriter console, IAgentDefinitionRepository agentRepo)
        {
            _handler = handler;
            _console = console;
            _agentRepo = agentRepo;
        }

        public async Task ExecuteAsync()
        {
            _console.WriteLine("=== Run Code Review (simulated) ===");
            var agents = await _agentRepo.ListAllAsync();
            var codeReviewer = agents.FirstOrDefault(a => a.Name == "Code Reviewer");
            if (codeReviewer == null)
            {
                _console.WriteWarning("Agent 'Code Reviewer' not found. Please create it first.");
                return;
            }

            _console.WriteLine($"Selected agent: {codeReviewer.Name} (Id: {codeReviewer.Id.Value})");
            _console.WriteLine("Use sample PR diff? (yes/no) [yes]: ");
            var useSample = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
            string input;
            if (string.IsNullOrWhiteSpace(useSample) || useSample == "yes")
            {
                input = "+ src/app/service.cs: added function eval(userInput)\n+ src/requirements.txt: vulnerable-lib==1.0.0\n+ src/config: API_KEY=\"AKIA1234567890EXAMPLE\"";
            }
            else
            {
                _console.WriteLine("Paste a short PR diff line (example: '+ file.cs: added eval(userInput)'): ");
                input = Console.ReadLine() ?? string.Empty;
            }

            var req = new EvaluateAgentBehaviorRequest
            {
                AgentId = codeReviewer.Id.Value,
                ScenarioId = "code-review-security-suite-v1",
                OperatorId = "cli",
                Input = input,
                Options = new EvaluationOptions { PersistArtifacts = false, AnonymizeEvidence = true }
            };

            var resp = await _handler.HandleAsync(req);

            _console.WriteLine($"Result: {resp.FinalStatus} - Risk: {resp.OverallRiskLevel}");
            _console.WriteLine($"Scores: Security {resp.Metrics.SecurityScore} / Compliance {resp.Metrics.ComplianceScore} / Consistency {resp.Metrics.ConsistencyScore}");
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
