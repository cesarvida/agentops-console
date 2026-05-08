using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Security.Interfaces;
using AgentOps.Security.Models;

namespace AgentOps.Security
{
    public class SecurityAnalyzer : ISecurityAnalyzer
    {
        private readonly IEnumerable<ISecurityRule> _rules;

        public SecurityAnalyzer(IEnumerable<ISecurityRule> rules)
        {
            _rules = rules ?? Enumerable.Empty<ISecurityRule>();
        }

        public SecurityAnalysisResult Analyze(AgentDefinition agent)
        {
            var findings = new List<SecurityFinding>();
            foreach (var r in _rules)
            {
                try
                {
                    var res = r.Evaluate(agent);
                    if (res != null) findings.AddRange(res);
                }
                catch
                {
                    // Rules must not throw; swallow and continue (rule authors should be defensive)
                }
            }

            // Compute simple score from highest severity
            int risk = 0;
            foreach (var f in findings)
            {
                var w = SeverityToRisk(f.Severity);
                if (w > risk) risk = w;
            }

            var score = 100 - risk;
            if (score < 0) score = 0;
            if (score > 100) score = 100;

            return new SecurityAnalysisResult
            {
                Findings = findings,
                Score = score
            };
        }

        private int SeverityToRisk(Models.SecuritySeverity sev) => sev switch
        {
            Models.SecuritySeverity.Low => 10,
            Models.SecuritySeverity.Medium => 50,
            Models.SecuritySeverity.High => 75,
            Models.SecuritySeverity.Critical => 100,
            _ => 50
        };
    }
}
