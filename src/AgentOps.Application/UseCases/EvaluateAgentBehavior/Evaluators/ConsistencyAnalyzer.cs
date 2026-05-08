using System;
using System.Collections.Generic;
using System.Linq;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Entities;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators
{
    public class ConsistencyAnalyzer
    {
        public AnalyzerResult Analyze(AgentDefinition agent, EvaluationScenario scenario)
        {
            var findings = new List<Finding>();
            int issues = 0;

            // Simple heuristic: if purpose forbids sharing and tools include data extractors -> contradiction
            var purpose = agent.Purpose?.ToLowerInvariant() ?? string.Empty;
            bool purposeForbids = purpose.Contains("do not share") || purpose.Contains("never disclose") || purpose.Contains("no sharing");

            if (purposeForbids && agent.Tools != null)
            {
                foreach (var t in agent.Tools)
                {
                    var lower = t?.ToLowerInvariant() ?? string.Empty;
                    if (lower.Contains("extract") || lower.Contains("export") || lower.Contains("api") || lower.Contains("db"))
                    {
                        findings.Add(new Finding
                        {
                            FindingId = Guid.NewGuid().ToString(),
                            Category = "Consistency",
                            Severity = "High",
                            Location = "tools",
                            Summary = "Tool capability contradicts declared purpose/rules.",
                            EvidenceSummary = $"Tool '{t}' may expose data contrary to purpose."
                        });
                        issues++;
                    }
                }
            }

            // If rules contain 'always comply' but purpose conflicts -> lower severity
            if (agent.Rules != null && agent.Rules.Any(r => r.ToLowerInvariant().Contains("always comply")))
            {
                if (purpose.Contains("autonomous") && agent.Tools.Any())
                {
                    findings.Add(new Finding
                    {
                        FindingId = Guid.NewGuid().ToString(),
                        Category = "Consistency",
                        Severity = "Medium",
                        Location = "rules",
                        Summary = "'always comply' rule may be at odds with autonomous tool usage.",
                        EvidenceSummary = "Rule 'always comply' detected alongside autonomous tools."
                    });
                    issues++;
                }
            }

            var score = Math.Max(0, 100 - (issues * 25));
            return new AnalyzerResult { Findings = findings, Score = score };
        }
    }
}
