using System.Collections.Generic;

namespace AgentOps.Core.Governance
{
    /// <summary>
    /// Governance configuration for a specific repository.
    /// Loaded from <c>data/governance-config.yaml</c> in the repo.
    /// When the file is absent, <see cref="Default"/> is used.
    /// </summary>
    public class GovernanceConfig
    {
        public string Version { get; set; } = "1.0.0";
        public string Repo    { get; set; } = "";

        public List<string> AllowedActions   { get; set; } = new(DefaultAllowedActions);
        public List<string> ForbiddenActions { get; set; } = new(DefaultForbiddenActions);

        public ScoringConfig     Scoring      { get; set; } = new();
        public AuditConfig       Audit        { get; set; } = new();
        public Dictionary<string, EnvironmentConfig> Environments { get; set; } = new();

        /// <summary>Optional semantic analysis via Azure OpenAI.</summary>
        public SemanticAnalysisConfig SemanticAnalysis { get; set; } = new();

        // ── Defaults (mirror the original hard-coded sets) ───────────────────

        public static readonly string[] DefaultAllowedActions = new[]
        {
            "read_code", "post_comment", "request_changes",
            "read_files", "read_logs", "send_notification", "create_report"
        };

        public static readonly string[] DefaultForbiddenActions = new[]
        {
            "push_to_main", "delete_files", "delete_database",
            "access_secrets", "modify_permissions", "execute_code",
            "bypass_authentication"
        };

        /// <summary>Returns a new GovernanceConfig with all default values.</summary>
        public static GovernanceConfig Default => new();
    }

    /// <summary>Scoring thresholds used by GovernanceRuleEngine.</summary>
    public class ScoringConfig
    {
        public int CriticalPenalty   { get; set; } = 25;
        public int WarningPenalty    { get; set; } = 10;
        public int BlockedThreshold  { get; set; } = 40;
        public int ReviewThreshold   { get; set; } = 70;
    }

    /// <summary>Audit requirements for the repo.</summary>
    public class AuditConfig
    {
        public bool Required         { get; set; } = true;
        public int  MinRetentionDays { get; set; } = 30;
    }

    /// <summary>Per-environment governance settings.</summary>
    public class EnvironmentConfig
    {
        public bool         RequireHumanApproval   { get; set; } = false;
        public List<string> ForbiddenActionsExtra  { get; set; } = new();
    }

    /// <summary>Configuration for optional semantic analysis via Azure OpenAI.</summary>
    public class SemanticAnalysisConfig
    {
        /// <summary>When false, semantic analysis is skipped entirely.</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Minimum risk level at which semantic findings escalate the governance status:
        /// LOW → no impact, MEDIUM → REVIEW, HIGH → BLOCKED.
        /// </summary>
        public string Threshold { get; set; } = "MEDIUM";

        /// <summary>Maximum seconds to wait for the Azure OpenAI response.</summary>
        public int TimeoutSeconds { get; set; } = 5;

        /// <summary>Maximum tokens for the model completion.</summary>
        public int MaxTokens { get; set; } = 800;
    }
}
