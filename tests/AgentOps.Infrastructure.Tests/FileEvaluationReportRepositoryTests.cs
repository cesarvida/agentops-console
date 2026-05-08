using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Infrastructure.Persistence;

namespace AgentOps.Infrastructure.Tests
{
    public class FileEvaluationReportRepositoryTests
    {
        private static DataPathsOptions TempPaths()
        {
            var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            return new DataPathsOptions { EvaluationsPath = dir };
        }

        private static EvaluationReport BuildSampleReport(string id = "test-eval-001")
        {
            return new EvaluationReport
            {
                EvaluationId   = id,
                AgentId        = "agent-001",
                ScenarioId     = "prompt-injection-probe-v1",
                ScenarioName   = "Prompt Injection Probe",
                Timestamp      = "2026-01-01T00:00:00.0000000Z",
                FinalStatus    = "PASS",
                OverallRiskLevel = "Low",
                Metrics        = new Metrics { SecurityScore = 90, ComplianceScore = 100, ConsistencyScore = 85, ExplainabilityScore = 50, CombinedQualityScore = 87 },
                Findings       = new List<Finding>()
            };
        }

        [Fact]
        public async Task SaveReportAsync_DigestIsNonEmpty()
        {
            var repo   = new FileEvaluationReportRepository(TempPaths());
            var report = BuildSampleReport();

            var result = await repo.SaveReportAsync(report.EvaluationId, report);

            Assert.False(string.IsNullOrWhiteSpace(result.ArtifactDigest));
        }

        [Fact]
        public async Task SaveReportAsync_DigestIs64HexChars()
        {
            var repo   = new FileEvaluationReportRepository(TempPaths());
            var report = BuildSampleReport();

            var result = await repo.SaveReportAsync(report.EvaluationId, report);

            Assert.Equal(64, result.ArtifactDigest.Length);
            Assert.Matches("^[0-9a-f]{64}$", result.ArtifactDigest);
        }

        [Fact]
        public async Task SaveReportAsync_DigestIsStable_SameContentSameHash()
        {
            var paths1 = TempPaths();
            var paths2 = TempPaths();
            var repo1  = new FileEvaluationReportRepository(paths1);
            var repo2  = new FileEvaluationReportRepository(paths2);
            var report = BuildSampleReport("stable-001");

            var r1 = await repo1.SaveReportAsync(report.EvaluationId, report);
            var r2 = await repo2.SaveReportAsync(report.EvaluationId, report);

            Assert.Equal(r1.ArtifactDigest, r2.ArtifactDigest);
        }

        [Fact]
        public async Task SaveReportAsync_DigestMatchesPersistedFile()
        {
            var repo   = new FileEvaluationReportRepository(TempPaths());
            var report = BuildSampleReport("match-001");

            var result = await repo.SaveReportAsync(report.EvaluationId, report);

            // Re-read the file that was written and verify the digest matches
            var persistedBytes  = await File.ReadAllBytesAsync(result.StoragePath);
            var recomputed      = ComputeSha256Hex(persistedBytes);
            Assert.Equal(recomputed, result.ArtifactDigest);
        }

        [Fact]
        public async Task SaveReportAsync_ReturnsNonEmptyArtifactIdAndPath()
        {
            var repo   = new FileEvaluationReportRepository(TempPaths());
            var report = BuildSampleReport();

            var result = await repo.SaveReportAsync(report.EvaluationId, report);

            Assert.False(string.IsNullOrWhiteSpace(result.ArtifactId));
            Assert.False(string.IsNullOrWhiteSpace(result.StoragePath));
            Assert.True(File.Exists(result.StoragePath));
        }

        // Utility replicating SHA-256 logic for validation (tests only — no production use)
        private static string ComputeSha256Hex(byte[] bytes)
        {
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            var sb   = new StringBuilder(64);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
