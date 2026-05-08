using System;
using System.Collections.Generic;

namespace AgentOps.Application.UseCases.ViewAuditTrail
{
    public class AuditEntrySummary
    {
        public string AuditId          { get; init; } = string.Empty;
        public string TimestampUtc     { get; init; } = string.Empty;
        public string Action           { get; init; } = string.Empty;
        public string AgentId          { get; init; } = string.Empty;
        public string EvaluationId     { get; init; } = string.Empty;
        public string ScenarioId       { get; init; } = string.Empty;
        public string FinalStatus      { get; init; } = string.Empty;
        public string OverallRiskLevel { get; init; } = string.Empty;
        public int    CombinedQualityScore    { get; init; }
        public int    CriticalFindingsCount   { get; init; }
        public int    FindingsCount           { get; init; }
    }

    public class ViewAuditTrailResponse
    {
        public IReadOnlyList<AuditEntrySummary> Entries { get; init; } = Array.Empty<AuditEntrySummary>();
        public bool HasMore { get; init; }
    }
}
