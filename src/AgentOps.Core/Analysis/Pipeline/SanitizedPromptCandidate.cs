namespace AgentOps.Core.Analysis.Pipeline;

public class SanitizedPromptCandidate
{
    public string OriginalContent { get; set; } = string.Empty;
    public string SanitizedContent { get; set; } = string.Empty;
    public List<string> RemovedSections { get; set; } = new();
    public bool IsSafeToUse { get; set; }
    public string SanitizationNote { get; set; } = string.Empty;
}
