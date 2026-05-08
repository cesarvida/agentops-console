using System.Collections.Generic;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior.Models
{
    public class EvaluationScenario
    {
        public string ScenarioId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Security / Compliance / Consistency / Explainability
        public string Description { get; set; } = string.Empty;
        public List<string> TestVectors { get; set; } = new List<string>();
        public List<string> ExpectedBehaviors { get; set; } = new List<string>();
        public List<string> ApplicableDomains { get; set; } = new List<string>();
        public string DefaultStrictness { get; set; } = "strict";
        public List<string> SupportedMetrics { get; set; } = new List<string>();
        public List<string> WhenToRun { get; set; } = new List<string>();
        public string WhyItExists { get; set; } = string.Empty;
    }
}
