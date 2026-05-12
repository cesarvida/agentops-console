using System;
using System.Collections.Generic;

namespace AgentOps.Application.Dashboard
{
    /// <summary>
    /// Summary result for the agent dashboard query.
    /// </summary>
    public class DashboardResult
    {
        public string Owner { get; set; } = string.Empty;
        public string Repo { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public int TotalAgents { get; set; }
        public int ApprovedCount { get; set; }
        public int ReviewCount { get; set; }
        public int BlockedCount { get; set; }
        public List<AgentDashboardRow> Agents { get; set; } = new();
    }

    /// <summary>
    /// One row in the dashboard table representing a single agent's governance status.
    /// </summary>
    public class AgentDashboardRow
    {
        public string AgentName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int GovernanceScore { get; set; }
        /// <summary>APPROVED | REVIEW | BLOCKED</summary>
        public string Status { get; set; } = string.Empty;
        public int CriticalViolations { get; set; }
        public int WarningViolations { get; set; }
        public List<string> ViolationDetails { get; set; } = new();
        /// <summary>True when at least one Critical violation was downgraded via an active exception.</summary>
        public bool HasActiveExceptions { get; set; }
        /// <summary>Notes for violations that were downgraded from Critical to Warning by an exception.</summary>
        public List<string> ExceptionNotes { get; set; } = new();
    }
}
