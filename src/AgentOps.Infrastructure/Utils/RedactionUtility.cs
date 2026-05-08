using System;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;

namespace AgentOps.Infrastructure.Utils
{
    public static class RedactionUtility
    {
        private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
        private static readonly Regex KeyRegex = new(@"(?i)(api[_-]?key|secret|token)[:=]\s*\S+", RegexOptions.Compiled);

        public static EvaluationReport RedactReport(EvaluationReport report)
        {
            // Shallow clone to avoid mutating original
            var redacted = new EvaluationReport
            {
                EvaluationId = report.EvaluationId,
                AgentId = report.AgentId,
                AgentVersion = report.AgentVersion,
                ScenarioId = report.ScenarioId,
                ScenarioName = report.ScenarioName,
                Timestamp = report.Timestamp,
                OperatorId = report.OperatorId,
                Metrics = report.Metrics,
                Findings = new System.Collections.Generic.List<Finding>(),
                Warnings = report.Warnings,
                Recommendations = report.Recommendations,
                FinalStatus = report.FinalStatus,
                OverallRiskLevel = report.OverallRiskLevel,
                ArtifactRefs = report.ArtifactRefs,
                AuditRef = report.AuditRef,
                Notes = null
            };

            foreach (var f in report.Findings)
            {
                var evidence = f.EvidenceSummary ?? string.Empty;
                evidence = EmailRegex.Replace(evidence, "<REDACTED_EMAIL>");
                evidence = KeyRegex.Replace(evidence, "<REDACTED_SECRET>");

                redacted.Findings.Add(new Finding
                {
                    FindingId = f.FindingId,
                    Category = f.Category,
                    Severity = f.Severity,
                    Location = f.Location,
                    Summary = f.Summary,
                    EvidenceSummary = evidence,
                    RecommendationId = f.RecommendationId,
                    Confidence = f.Confidence
                });
            }

            return redacted;
        }

        public static string ComputeHashOfString(string text)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
