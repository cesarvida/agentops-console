using System.Collections.Generic;
using AgentOps.Core.Governance;

namespace AgentOps.Core.Rules
{
    /// <summary>
    /// User-defined rules that can override system defaults.
    /// Can be loaded from YAML files or CLI flags.
    /// </summary>
    public class UserRules
    {
        public string Name { get; set; } = "Custom Rules";
        public string Description { get; set; } = "User-defined rules";

        public List<string> AllowedActions { get; set; } = new();
        public List<string> ForbiddenActions { get; set; } = new();

        public bool OwnerRequired { get; set; } = true;
        public bool AuditRequired { get; set; } = true;
        public bool VersionRequired { get; set; } = true;

        public int CriticalPenalty { get; set; } = 25;
        public int WarningPenalty { get; set; } = 10;
        public int BlockedThreshold { get; set; } = 40;
        public int ReviewThreshold { get; set; } = 70;

        /// <summary>
        /// Converts UserRules to GovernanceConfig so the governance engine can use them.
        /// </summary>
        public GovernanceConfig ToGovernanceConfig()
        {
            var config = new GovernanceConfig
            {
                AllowedActions = new List<string>(AllowedActions),
                ForbiddenActions = new List<string>(ForbiddenActions),
                Scoring = new ScoringConfig
                {
                    CriticalPenalty = CriticalPenalty,
                    WarningPenalty = WarningPenalty,
                    BlockedThreshold = BlockedThreshold,
                    ReviewThreshold = ReviewThreshold
                },
                Audit = new AuditConfig
                {
                    Required = AuditRequired
                }
            };

            return config;
        }

        /// <summary>
        /// Returns default user rules (equivalent to GovernanceConfig defaults).
        /// </summary>
        public static UserRules GetDefaults()
        {
            return new UserRules
            {
                Name = "Default Rules",
                Description = "System default rules",
                AllowedActions = new List<string>(GovernanceConfig.DefaultAllowedActions),
                ForbiddenActions = new List<string>(GovernanceConfig.DefaultForbiddenActions),
                OwnerRequired = true,
                AuditRequired = true,
                VersionRequired = true,
                CriticalPenalty = 25,
                WarningPenalty = 10,
                BlockedThreshold = 40,
                ReviewThreshold = 70
            };
        }
    }
}
