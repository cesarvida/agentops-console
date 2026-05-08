using System;

namespace AgentOps.Security.Models
{
    public class SecurityFinding
    {
        public string FindingId { get; set; } = Guid.NewGuid().ToString();
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public SecuritySeverity Severity { get; set; } = SecuritySeverity.Low;
        public string Location { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string EvidenceSummary { get; set; } = string.Empty;
        public string? Evidence { get; set; }
        public string? Recommendation { get; set; }
    }
}
