namespace AgentOps.Core.Analysis;

/// <summary>
/// Context classification result, used by detectors to adjust confidence scores.
/// </summary>
public class ContentContext
{
    public bool IsDocumentation { get; set; }
    public bool IsTestFile { get; set; }
    public bool IsCodeExample { get; set; }
    public bool IsActivePrompt { get; set; }
    public float MaliciousIntentScore { get; set; }
}
