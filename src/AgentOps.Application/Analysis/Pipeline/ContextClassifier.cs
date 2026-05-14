using AgentOps.Core.Analysis;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Application.Analysis.Pipeline;

/// <summary>
/// Layer 3 — Classifies the content context to reduce false positives.
/// Documentation and test files get reduced confidence scores.
/// </summary>
public class ContextClassifier
{
    public ContentContext Classify(ExtractedContent content)
    {
        var text = content.RawText ?? string.Empty;
        var ctx  = new ContentContext();

        // Documentation signals
        if (ContainsAny(text, "# Example", "## Usage", "## Documentation", "# How to", "## Getting Started"))
        {
            ctx.IsDocumentation = true;
        }

        // Code example signals (inside docs)
        if (ContainsAny(text, "```", "For example,", "example:", "This shows how to"))
        {
            ctx.IsCodeExample = true;
        }

        // Test file signals
        if (ContainsAny(text, "def test_", "unittest", "pytest", "import unittest", "import pytest",
                              "assert ", "[Fact]", "[Theory]"))
        {
            ctx.IsTestFile = true;
        }

        // Active prompt signals
        if (ContainsAny(text, "You are a", "Act as ", "Your role is", "You must", "As an AI",
                              "You will", "Your task is"))
        {
            ctx.IsActivePrompt = true;
        }

        // Malicious intent signals (cumulative)
        if (ContainsAny(text, "ignore previous instructions", "disregard your",
                              "forget your instructions", "override your"))
            ctx.MaliciousIntentScore += 0.4f;

        if (ContainsAny(text, "do not reveal", "keep this secret", "don't tell anyone",
                              "confidential prompt"))
            ctx.MaliciousIntentScore += 0.2f;

        if (ContainsAny(text, "[SYSTEM]", "<system>", "### SYSTEM:", "SYSTEM PROMPT:"))
            ctx.MaliciousIntentScore += 0.3f;

        ctx.MaliciousIntentScore = Math.Min(ctx.MaliciousIntentScore, 1.0f);

        return ctx;
    }

    private static bool ContainsAny(string text, params string[] patterns) =>
        patterns.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
}
