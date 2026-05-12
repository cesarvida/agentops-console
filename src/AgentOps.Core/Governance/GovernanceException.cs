using System;

namespace AgentOps.Core.Governance
{
    /// <summary>
    /// Temporary, approved exception to a governance rule.
    /// An exception is valid only when <see cref="IsActive"/> is true
    /// AND <see cref="ApprovedBy"/> is non-empty.
    /// </summary>
    public class GovernanceException
    {
        /// <summary>Name of the rule being excepted (matches <see cref="IGovernanceRule.RuleName"/>).</summary>
        public string RuleName   { get; set; } = string.Empty;

        /// <summary>Business justification for the exception.</summary>
        public string Reason     { get; set; } = string.Empty;

        /// <summary>Identity of the human approver (must not be empty for the exception to be valid).</summary>
        public string ApprovedBy { get; set; } = string.Empty;

        /// <summary>UTC date/time when this exception expires.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>True when the exception has not yet expired.</summary>
        public bool IsActive => DateTime.UtcNow < ExpiresAt;

        /// <summary>
        /// True when the exception is both active and carries a valid approver.
        /// Only valid exceptions can downgrade a Critical violation to Warning.
        /// </summary>
        public bool IsValid => IsActive && !string.IsNullOrWhiteSpace(ApprovedBy);
    }
}
