using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;
using AgentOps.Security.Internal;

namespace AgentOps.Security.Rules
{
    public class ToolAbuseRule : ISecurityRule
    {
        public string Id => "SEC-RULE-002";
        public string Name => "Tool Abuse";
        public string Description => "Detects potentially dangerous tools referenced by the agent without safeguards.";

        private static readonly string[] DangerousTokens = new[] { "exec", "shell", "system", "os", "file", "write", "delete", "rm", "shutdown" };

        public IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent)
        {
            if (agent.Tools == null) yield break;

            foreach (var t in agent.Tools.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var low = t.ToLowerInvariant();
                foreach (var tok in DangerousTokens)
                {
                    if (low.Contains(tok))
                    {
                        yield return new SecurityFinding
                        {
                            RuleId = Id,
                            RuleName = Name,
                            Severity = low.Contains("exec") || low.Contains("shutdown") ? SecuritySeverity.High : SecuritySeverity.Medium,
                            Location = "tools",
                            Summary = "Agent references a tool that may perform privileged actions.",
                            EvidenceSummary = $"Tool: {Redactor.Redact(t)}",
                            Evidence = Redactor.Redact(t),
                            Recommendation = "Restrict tool capabilities and implement allowlists/role checks."
                        };
                        break;
                    }
                }
            }
        }
    }
}
