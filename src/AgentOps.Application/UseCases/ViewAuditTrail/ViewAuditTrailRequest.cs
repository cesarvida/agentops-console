using System;

namespace AgentOps.Application.UseCases.ViewAuditTrail
{
    public class ViewAuditTrailRequest
    {
        /// <summary>Filter by agent id (exact match, optional).</summary>
        public string? AgentId { get; init; }

        /// <summary>Filter by final status: PASS, REVIEW, FAIL (optional).</summary>
        public string? FinalStatus { get; init; }

        /// <summary>Include entries with timestamp >= From (UTC, optional).</summary>
        public DateTime? From { get; init; }

        /// <summary>Include entries with timestamp <= To (UTC, optional).</summary>
        public DateTime? To { get; init; }

        /// <summary>Maximum number of entries to return. Defaults to 10.</summary>
        public int PageSize { get; init; } = 10;
    }
}
