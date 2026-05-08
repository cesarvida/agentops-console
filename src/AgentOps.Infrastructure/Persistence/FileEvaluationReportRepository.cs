using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;

namespace AgentOps.Infrastructure.Persistence
{
    public class FileEvaluationReportRepository : IEvaluationReportRepository
    {
        private readonly string _basePath;

        public FileEvaluationReportRepository(DataPathsOptions paths)
        {
            _basePath = paths.EvaluationsPath ?? "./data/evaluations";
            Directory.CreateDirectory(_basePath);
        }

        public async Task<SaveReportResult> SaveReportAsync(string evaluationId, EvaluationReport report)
        {
            // 1. Redact before persisting (Infrastructure responsibility)
            var redacted = Utils.RedactionUtility.RedactReport(report);

            // 2. Serialize to UTF-8 bytes — these are the exact bytes written to disk
            var json    = JsonSerializer.Serialize(redacted, new JsonSerializerOptions { WriteIndented = true });
            var content = Encoding.UTF8.GetBytes(json);

            // 3. Persist
            var fileName = $"evaluation_{evaluationId}.json";
            var filePath = Path.Combine(_basePath, fileName);
            await File.WriteAllBytesAsync(filePath, content);

            // 4. SHA-256 of the persisted bytes (after redaction, not before)
            var digest = ComputeSha256Hex(content);
            var artifactId = $"artifact-{evaluationId}";

            return new SaveReportResult
            {
                StoragePath   = filePath,
                ArtifactId    = artifactId,
                ArtifactDigest = digest
            };
        }

        private static string ComputeSha256Hex(byte[] bytes)
        {
            var hash = SHA256.HashData(bytes);
            var sb = new StringBuilder(64);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
