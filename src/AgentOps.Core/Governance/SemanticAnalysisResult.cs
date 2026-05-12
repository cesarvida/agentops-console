using System.Collections.Generic;

namespace AgentOps.Core.Governance
{
    /// <summary>
    /// Result of an optional Azure OpenAI semantic governance analysis.
    /// When <see cref="IsAvailable"/> is false the analysis was skipped or failed;
    /// the rule-based governance result is authoritative in that case.
    /// </summary>
    public class SemanticAnalysisResult
    {
        /// <summary>Risk level returned by the model: LOW, MEDIUM, or HIGH.</summary>
        public string RiskLevel { get; set; } = "LOW";

        /// <summary>Issues identified by the model.</summary>
        public List<string> Issues { get; set; } = new();

        /// <summary>Recommendations from the model.</summary>
        public List<string> Recommendations { get; set; } = new();

        /// <summary>
        /// True when the analysis ran successfully and the result is trustworthy.
        /// False when skipped, timed out, or the model returned an unparseable response.
        /// </summary>
        public bool IsAvailable { get; set; } = false;

        /// <summary>Human-readable reason why the analysis was skipped/failed.</summary>
        public string? ErrorMessage { get; set; }

        /// <summary>Returns a skipped result with a descriptive reason.</summary>
        public static SemanticAnalysisResult Skipped(string reason) => new()
        {
            IsAvailable  = false,
            ErrorMessage = reason
        };
    }
}
