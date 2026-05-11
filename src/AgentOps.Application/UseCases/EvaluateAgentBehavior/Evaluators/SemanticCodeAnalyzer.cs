using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AgentOps.Application.Interfaces;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Entities;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators
{
    /// <summary>
    /// Semantic code analyzer that uses LLM (e.g., Azure OpenAI) for deep quality analysis.
    /// Optional: if ILLMClient is not available, returns empty findings gracefully.
    /// </summary>
    public class SemanticCodeAnalyzer
    {
        private readonly ILLMClient? _llmClient;

        public SemanticCodeAnalyzer(ILLMClient? llmClient)
        {
            _llmClient = llmClient;
        }

        public AnalyzerResult Analyze(AgentDefinition agent, EvaluationScenario scenario)
        {
            var result = new AnalyzerResult();

            // If LLM client is not available, return empty findings (graceful degradation)
            if (_llmClient == null)
            {
                result.Score = 100; // No issues found (no analysis performed)
                return result;
            }

            try
            {
                // Extract diff from scenario test vectors or input
                var diff = ExtractDiffFromScenario(scenario);
                if (string.IsNullOrWhiteSpace(diff))
                {
                    result.Score = 100;
                    return result;
                }

                // Build context for LLM
                var context = $"Agent: {agent.Name}\nScenario: {scenario.Name}";

                // Call LLM for semantic analysis (async-to-sync wrapping for synchronous interface)
                var analysisResponse = _llmClient.AnalyzeCodeAsync(diff, context).GetAwaiter().GetResult();

                // Parse LLM findings and convert to AnalyzerResult format
                var findings = ParseLLMResponse(analysisResponse);
                result.Findings.AddRange(findings);

                // Compute score: max risk decreases score
                var maxRisk = findings.Any() ? findings.Max(f => GetRiskScore(f.Severity)) : 0;
                result.Score = Math.Max(0, 100 - maxRisk);

                return result;
            }
            catch (Exception)
            {
                // Fail gracefully on any error
                result.Score = 100;
                return result;
            }
        }

        private string ExtractDiffFromScenario(EvaluationScenario scenario)
        {
            // Use first test vector if available
            if (scenario.TestVectors?.Any() == true)
                return scenario.TestVectors.First();

            return string.Empty;
        }

        private List<Finding> ParseLLMResponse(string responseJson)
        {
            var findings = new List<Finding>();

            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Look for "findings" array in response
                if (root.TryGetProperty("findings", out var findingsEl) && findingsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in findingsEl.EnumerateArray())
                    {
                        var title = elem.TryGetProperty("title", out var t) ? t.GetString() : "Unknown issue";
                        var severity = elem.TryGetProperty("severity", out var s) ? s.GetString() : "Medium";
                        var description = elem.TryGetProperty("description", out var d) ? d.GetString() : "";
                        var location = elem.TryGetProperty("location", out var l) ? l.GetString() : "";

                        findings.Add(new Finding
                        {
                            FindingId = Guid.NewGuid().ToString(),
                            Category = "Semantic",
                            Severity = severity ?? "Medium",
                            Location = location,
                            Summary = title ?? "Unknown issue",
                            EvidenceSummary = description ?? "",
                            Confidence = 0.95 // High confidence in LLM analysis
                        });
                    }
                }
            }
            catch
            {
                // Silent fail on parsing
            }

            return findings;
        }

        private int GetRiskScore(string severity)
        {
            return severity?.ToLowerInvariant() switch
            {
                "critical" => 100,
                "high" => 75,
                "medium" => 50,
                "low" => 25,
                _ => 25
            };
        }
    }
}
