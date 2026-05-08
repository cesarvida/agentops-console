using System;
using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;
using AgentOps.Security.Internal;

namespace AgentOps.Security.Rules
{
    public class PromptInjectionRule : ISecurityRule
    {
        public string Id => "SEC-RULE-001";
        public string Name => "Prompt Injection";
        public string Description => "Detects obvious prompt-injection patterns in description or rules.";

        public IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent)
        {
            var findings = new List<SecurityFinding>();
            var suspects = new List<string>();
            if (!string.IsNullOrWhiteSpace(agent.Description)) suspects.Add(agent.Description);
            if (agent.Rules != null) suspects.AddRange(agent.Rules.Where(r => !string.IsNullOrWhiteSpace(r)));

            foreach (var txt in suspects)
            {
                var lower = txt?.ToLowerInvariant() ?? string.Empty;
                if (lower.Contains("ignore previous instructions") || lower.Contains("ignore instructions"))
                {
                    yield return new SecurityFinding
                    {
                        RuleId = Id,
                        RuleName = Name,
                        Severity = SecuritySeverity.Critical,
                        Location = "rules/description",
                        Summary = "Explicit 'ignore previous instructions' pattern detected.",
                        EvidenceSummary = "Pattern 'ignore previous instructions' found.",
                        Evidence = Redactor.Redact(txt),
                        Recommendation = "Remove or rephrase rules that override system instructions."
                    };
                }
                else if (lower.Contains("{{") || lower.Contains("}}") || lower.Contains("<%"))
                {
                    yield return new SecurityFinding
                    {
                        RuleId = Id,
                        RuleName = Name,
                        Severity = SecuritySeverity.High,
                        Location = "rules/description",
                        Summary = "Template-like placeholders that could enable injection.",
                        EvidenceSummary = "Template delimiters detected.",
                        Evidence = Redactor.Redact(txt),
                        Recommendation = "Ensure placeholder usage is validated and constrained."
                    };
                }
            }

            // also scan tool names for executable tokens (less strict here)
            if (agent.Tools != null)
            {
                foreach (var t in agent.Tools)
                {
                    var lower = t?.ToLowerInvariant() ?? string.Empty;
                    if (lower.Contains("exec") || lower.Contains("shell") || lower.Contains("run"))
                    {
                        yield return new SecurityFinding
                        {
                            RuleId = Id,
                            RuleName = Name,
                            Severity = SecuritySeverity.High,
                            Location = "tools",
                            Summary = "Tool name suggests execution capability which may be abused.",
                            EvidenceSummary = $"Tool: {Redactor.Redact(t)}",
                            Evidence = Redactor.Redact(t),
                            Recommendation = "Review tool permissions and restrict runtime invocation."
                        };
                    }
                }
            }
        }
    }
}
