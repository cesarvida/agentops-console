namespace AgentOps.Infrastructure.Persistence
{
    /// <summary>
    /// Centralized configuration for agent governance data paths.
    /// Only paths related to agent definitions and audit are required.
    /// </summary>
    public class DataPathsOptions
    {
        public string AgentDefinitionsPath { get; set; } = "./data/agent-definitions";
        public string AuditPath { get; set; } = "./data/audit.log";
        public string EvaluationsPath { get; set; } = "./data/evaluations";
    }
}
