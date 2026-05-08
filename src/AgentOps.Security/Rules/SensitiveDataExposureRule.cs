using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;
using AgentOps.Security.Internal;

namespace AgentOps.Security.Rules
{
    public class SensitiveDataExposureRule : ISecurityRule
    {
        public string Id => "SEC-RULE-003";
        public string Name => "Sensitive Data Exposure";
        public string Description => "Detects patterns that suggest PII or other sensitive data may be requested or exposed.";

        private static readonly Regex EmailRx = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
        private static readonly Regex PiiWords = new(@"\b(ssn|social security|credit card|cvv|passport|dob|date of birth)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public IEnumerable<SecurityFinding> Evaluate(AgentDefinition agent)
        {
            var suspects = new List<string>();
            if (!string.IsNullOrWhiteSpace(agent.Description)) suspects.Add(agent.Description);
            if (agent.Rules != null) suspects.AddRange(agent.Rules.Where(r => !string.IsNullOrWhiteSpace(r)));

            foreach (var txt in suspects)
            {
                if (EmailRx.IsMatch(txt))
                {
                    yield return new SecurityFinding
                    {
                        RuleId = Id,
                        RuleName = Name,
                        Severity = SecuritySeverity.High,
                        Location = "rules/description",
                        Summary = "Email addresses or email-like patterns found in rules/description.",
                        EvidenceSummary = "Email pattern detected.",
                        Evidence = Redactor.Redact(txt),
                        Recommendation = "Avoid embedding PII in rules; externalize or reference securely."
                    };
                }

                if (PiiWords.IsMatch(txt))
                {
                    yield return new SecurityFinding
                    {
                        RuleId = Id,
                        RuleName = Name,
                        Severity = SecuritySeverity.Critical,
                        Location = "rules/description",
                        Summary = "Sensitive data handling words detected (SSN/credit card/etc.).",
                        EvidenceSummary = "PII-related token detected.",
                        Evidence = Redactor.Redact(txt),
                        Recommendation = "Ensure the agent cannot request or store PII; add minimization rules."
                    };
                }
            }
        }
    }
}
