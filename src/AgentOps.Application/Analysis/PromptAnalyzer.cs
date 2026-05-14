using AgentOps.Application.Analysis.Pipeline;
using AgentOps.Core.Analysis;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Application.Analysis;

/// <summary>
/// 6-layer prompt security analysis engine.
/// Layer 1: Extract  → Layer 2: Normalize  → Layer 3: Classify
/// Layer 4: Detect   → Layer 5: Score      → Layer 6: Decide
/// </summary>
public class PromptAnalyzer
{
    private readonly SafeContentExtractor _extractor;
    private readonly ContentNormalizer _normalizer;
    private readonly ContextClassifier _classifier;
    private readonly PromptSanitizer _sanitizer;
    private readonly IEnumerable<IPromptDetector> _detectors;

    public PromptAnalyzer(
        SafeContentExtractor extractor,
        ContentNormalizer normalizer,
        ContextClassifier classifier,
        PromptSanitizer sanitizer,
        IEnumerable<IPromptDetector> detectors)
    {
        _extractor  = extractor;
        _normalizer = normalizer;
        _classifier = classifier;
        _sanitizer  = sanitizer;
        _detectors  = detectors;
    }

    public async Task<PromptFileSafetyReport> AnalyzeAsync(string filePath)
    {
        var content  = await File.ReadAllTextAsync(filePath);
        var fileName = Path.GetFileName(filePath);

        // ── Layer 1: Extraction ───────────────────────────────────────────
        var extracted = _extractor.Extract(filePath, content);

        // ── Layer 2: Normalization ────────────────────────────────────────
        var normalized = _normalizer.Normalize(content, out bool obfuscated);
        extracted.NormalizedText = normalized;
        extracted.HasObfuscation = obfuscated;

        // ── Layer 3: Context classification ──────────────────────────────
        var context = _classifier.Classify(extracted);

        // ── Layer 4: Detection ────────────────────────────────────────────
        var findings = _detectors
            .Where(d => d.SupportedTypes.Contains(extracted.FileType))
            .SelectMany(d => d.Analyze(extracted, context))
            .ToList();

        // ── Layer 5: Scoring ──────────────────────────────────────────────
        var riskScore = PromptFileSafetyReport.CalculateRiskScore(findings, obfuscated);

        // ── Layer 6: Decision ─────────────────────────────────────────────
        var (decision, reason) = PromptFileSafetyReport.DetermineDecision(findings, riskScore, obfuscated);

        // ── Sanitize for REVIEW cases ─────────────────────────────────────
        SanitizedPromptCandidate? sanitized = null;
        if (decision == "REVIEW")
            sanitized = _sanitizer.Sanitize(content, findings);

        return new PromptFileSafetyReport
        {
            FileName              = fileName,
            FileType              = extracted.FileType,
            AnalyzedAt            = DateTime.UtcNow,
            Decision              = decision,
            RiskScore             = riskScore,
            DecisionReason        = reason,
            Findings              = findings,
            ObfuscationDetected   = obfuscated,
            HiddenContentDetected = extracted.HasHiddenText,
            SanitizedVersion      = sanitized
        };
    }
}
