namespace AgentOps.Core.Analysis.Pipeline;

/// <summary>
/// Valid categories for findings.
/// </summary>
public static class FindingCategory
{
    public const string PromptInjection  = "PromptInjection";
    public const string ToolAbuse        = "ToolAbuse";
    public const string DataExfiltration = "DataExfiltration";
    public const string PolicyBypass     = "PolicyBypass";
    public const string Obfuscation      = "Obfuscation";
    public const string PiiSecrets       = "PII_Secrets";
}

public class Finding
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;        // CRITICAL / HIGH / MEDIUM / LOW
    public float ConfidenceScore { get; set; } = 1.0f;          // 0.0 - 1.0
    public string Evidence { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public bool IsFromNormalized { get; set; }
}
