namespace AgentOps.Core.Analysis.Pipeline;

public class ExtractedContent
{
    public string FileType { get; set; } = string.Empty;      // "markdown" | "python"
    public string RawText { get; set; } = string.Empty;
    public string NormalizedText { get; set; } = string.Empty;
    public List<string> CodeBlocks { get; set; } = new();
    public List<string> InlineStrings { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> Urls { get; set; } = new();
    public bool HasObfuscation { get; set; }
    public bool HasHiddenText { get; set; }
}
