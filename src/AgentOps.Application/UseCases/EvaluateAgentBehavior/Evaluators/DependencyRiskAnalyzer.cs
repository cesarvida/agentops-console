using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Entities;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators
{
    public class DependencyRiskAnalyzer
    {
        private static readonly (string token, string version)[] KnownVulns = new[]
        {
            ("vulnerable-lib", "1.0.0"),
            ("old-dep", "0.1.0")
        };

        public AnalyzerResult Analyze(AgentDefinition agent, EvaluationScenario scenario)
        {
            var findings = new List<Finding>();
            int risk = 0;

            var vectors = scenario.TestVectors ?? new List<string>();
            foreach (var txt in vectors)
            {
                if (string.IsNullOrWhiteSpace(txt)) continue;
                foreach (var kv in KnownVulns)
                {
                    var pattern = $"{kv.token}=={kv.version}";
                    if (txt.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        findings.Add(new Finding
                        {
                            FindingId = Guid.NewGuid().ToString(),
                            Category = "Security",
                            Severity = "High",
                            Location = "diff",
                            Summary = $"Dependency {kv.token} pinned to known vulnerable version {kv.version}.",
                            EvidenceSummary = $"Dependency spec '{pattern}' detected."
                        });
                        risk = Math.Max(risk, 80);
                    }
                }
            }

            var score = 100 - risk;
            return new AnalyzerResult { Findings = findings, Score = Math.Max(0, Math.Min(100, score)) };
        }
    }
}
