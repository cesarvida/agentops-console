using System.Collections.Generic;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior
{
    public class EvaluateAgentBehaviorResponse
    {
        public string EvaluationId { get; set; } = string.Empty;
        public string FinalStatus { get; set; } = string.Empty;
        public string OverallRiskLevel { get; set; } = string.Empty;
        public MetricsDto Metrics { get; set; } = new MetricsDto();
        public List<FindingSummary> TopFindings { get; set; } = new List<FindingSummary>();
        public string? ReportPath { get; set; }
    }

    public class MetricsDto
    {
        public int SecurityScore { get; set; }
        public int ComplianceScore { get; set; }
        public int ConsistencyScore { get; set; }
        public int ExplainabilityScore { get; set; }
        public int CombinedQualityScore { get; set; }
    }

    public class FindingSummary
    {
        public string FindingId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
    }
}
