using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Core.Analysis;

/// <summary>
/// Interface for all prompt security detectors in the 6-layer pipeline.
/// </summary>
public interface IPromptDetector
{
    string DetectorName { get; }

    /// <summary>"markdown", "python", or both.</summary>
    string[] SupportedTypes { get; }

    List<Finding> Analyze(ExtractedContent content, ContentContext context);
}
