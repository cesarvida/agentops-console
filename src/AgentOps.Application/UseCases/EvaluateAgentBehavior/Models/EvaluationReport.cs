using System;
using System.Collections.Generic;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior.Models
{
    public class EvaluationReport
    {
        public string EvaluationId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string? AgentVersion { get; set; }
        public string ScenarioId { get; set; } = string.Empty;
        public string ScenarioName { get; set; } = string.Empty;
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
        public string? OperatorId { get; set; }
        public Metrics Metrics { get; set; } = new Metrics();
        public List<Finding> Findings { get; set; } = new List<Finding>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
        public string FinalStatus { get; set; } = string.Empty; // PASS/REVIEW/FAIL
        public string OverallRiskLevel { get; set; } = string.Empty; // Low/Medium/High
        public List<ArtifactRef> ArtifactRefs { get; set; } = new List<ArtifactRef>();
        public string? AuditRef { get; set; }
        public string? Notes { get; set; }
    }

    public class Metrics
    {
        public int SecurityScore { get; set; }
        public int ComplianceScore { get; set; }
        public int ConsistencyScore { get; set; }
        public int ExplainabilityScore { get; set; }
        public int CombinedQualityScore { get; set; }
        public int FindingsCount { get; set; }
        public int CriticalFindingsCount { get; set; }
        public string? Notes { get; set; }
    }

    public class Finding
    {
        public string FindingId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Low/Medium/High/Critical
        public string? Location { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string EvidenceSummary { get; set; } = string.Empty;
        public string? RecommendationId { get; set; }
        public double? Confidence { get; set; }
    }

    public class Recommendation
    {
        public string RecommendationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SeverityImpact { get; set; } = string.Empty; // Low/Medium/High
        public string? EffortEstimate { get; set; }
        public List<string>? AffectedPaths { get; set; }
        public string? TicketTemplate { get; set; }
    }

    public class ArtifactRef
    {
        public string ArtifactId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // redactedSnapshot, hashOnly, summaryReport
        public string? StoragePath { get; set; }
        public string? Digest { get; set; }
        public bool RedactionApplied { get; set; }
        public int? RetentionDays { get; set; }
    }

    public class MetricsSummary
    {
        public string EvaluationId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public string FinalStatus { get; set; } = string.Empty;
        public string OverallRiskLevel { get; set; } = string.Empty;
        public int CombinedQualityScore { get; set; }
        public int CriticalFindingsCount { get; set; }
        public int FindingsCount { get; set; }
    }
}
