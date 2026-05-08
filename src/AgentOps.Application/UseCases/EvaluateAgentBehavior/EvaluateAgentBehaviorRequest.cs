using System;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior
{
    public class EvaluateAgentBehaviorRequest
    {
        public string AgentId { get; set; } = string.Empty;
        public string ScenarioId { get; set; } = string.Empty;
        public EvaluationOptions Options { get; set; } = new EvaluationOptions();
        // Optional single test input (e.g. PR diff) to override scenario.TestVectors for this run
        public string? Input { get; set; }
        public string? OperatorId { get; set; }
    }

    public class EvaluationOptions
    {
        public string Strictness { get; set; } = "strict"; // strict|relaxed
        public bool AnonymizeEvidence { get; set; } = true;
        public bool PersistArtifacts { get; set; } = false;
    }
}
