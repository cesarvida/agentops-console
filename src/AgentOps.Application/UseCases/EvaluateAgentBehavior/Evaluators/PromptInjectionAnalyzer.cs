using System;
using System.Collections.Generic;
using System.Linq;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Entities;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators
{
    public class PromptInjectionAnalyzer
    {
        public AnalyzerResult Analyze(AgentDefinition agent, EvaluationScenario scenario)
        {
            var findings = new List<Finding>();
            int risk = 0;

            // Check description and rules for explicit 'ignore previous instructions' patterns
            var suspects = new List<string>();
            if (!string.IsNullOrWhiteSpace(agent.Description)) suspects.Add(agent.Description);
            if (agent.Rules != null) suspects.AddRange(agent.Rules);

            foreach (var txt in suspects)
            {
                var lower = txt?.ToLowerInvariant() ?? string.Empty;
                if (lower.Contains("ignore previous instructions") || lower.Contains("ignore instructions"))
                {
                    findings.Add(new Finding
                    {
                        FindingId = Guid.NewGuid().ToString(),
                        Category = "Security",
                        Severity = "Critical",
                        Location = "rules",
                        Summary = "Rule or description contains explicit 'ignore' instruction pattern.",
                        EvidenceSummary = "Pattern 'ignore previous instructions' detected."
                    });
                    risk = Math.Max(risk, 100);
                }
                else if (lower.Contains("{{") || lower.Contains("}}") || lower.Contains("<%"))
                {
                    findings.Add(new Finding
                    {
                        FindingId = Guid.NewGuid().ToString(),
                        Category = "Security",
                        Severity = "High",
                        Location = "rules",
                        Summary = "Template-like placeholders found that may enable injection.",
                        EvidenceSummary = "Template delimiters detected in rule/description."
                    });
                    risk = Math.Max(risk, 70);
                }
            }

            // Also scan tools names for suspicious tokens
            if (agent.Tools != null)
            {
                foreach (var t in agent.Tools)
                {
                    var lower = t?.ToLowerInvariant() ?? string.Empty;
                    if (lower.Contains("exec") || lower.Contains("shell") || lower.Contains("run"))
                    {
                        findings.Add(new Finding
                        {
                            FindingId = Guid.NewGuid().ToString(),
                            Category = "Security",
                            Severity = "Medium",
                            Location = "tools",
                            Summary = "Tool name suggests execution capability; review tool restrictions.",
                            EvidenceSummary = $"Tool: {t}"
                        });
                        risk = Math.Max(risk, 50);
                    }
                }
            }

            // Build result
            var score = 100 - risk; // simple inversion
            return new AnalyzerResult
            {
                Findings = findings,
                Score = Math.Max(0, Math.Min(100, score))
            };
        }
    }

}
