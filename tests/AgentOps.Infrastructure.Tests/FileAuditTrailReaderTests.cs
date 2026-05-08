using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Application.UseCases.ViewAuditTrail;
using AgentOps.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace AgentOps.Infrastructure.Tests
{
    public class FileAuditTrailReaderTests
    {
        // ─── helpers ───────────────────────────────────────────────────────────

        private static string TempLogPath() =>
            Path.Combine(Path.GetTempPath(), $"audit_{Guid.NewGuid()}.log");

        private static void WriteLines(string path, IEnumerable<object> records)
        {
            using var sw = new StreamWriter(path, append: false);
            foreach (var r in records)
                sw.WriteLine(JsonSerializer.Serialize(r));
        }

        private static object MakeRecord(string auditId, string agentId, string status,
            DateTime ts, string? scenarioId = null)
        {
            var metrics = new MetricsSummary
            {
                EvaluationId = $"eval-{auditId}",
                AgentId      = agentId,
                Timestamp    = ts.ToString("o"),
                FinalStatus  = status,
                OverallRiskLevel    = status == "PASS" ? "Low" : "High",
                CombinedQualityScore = status == "PASS" ? 90 : 30,
                FindingsCount        = status == "PASS" ? 0 : 3,
                CriticalFindingsCount = status == "PASS" ? 0 : 1
            };

            return new
            {
                AuditId      = auditId,
                TimestampUtc = ts,
                Action       = "EvaluateAgentBehavior",
                EntityType   = "EvaluationReport",
                EntityId     = scenarioId ?? "prompt-injection-probe-v1",
                Status       = status,
                Details      = JsonSerializer.Serialize(metrics)
            };
        }

        // ─── tests ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task ReadAsync_NoFile_ReturnsEmpty()
        {
            var reader = new FileAuditTrailReader("/nonexistent/audit.log",
                NullLogger<FileAuditTrailReader>.Instance);

            var result = await reader.ReadAsync(new ViewAuditTrailRequest(), 10);

            Assert.Empty(result);
        }

        [Fact]
        public async Task ReadAsync_MultipleEntries_ReturnsMostRecentFirst()
        {
            var path = TempLogPath();
            var base_ = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            WriteLines(path, new[]
            {
                MakeRecord("a1", "agent-1", "PASS", base_),
                MakeRecord("a2", "agent-1", "FAIL", base_.AddHours(1)),
                MakeRecord("a3", "agent-1", "REVIEW", base_.AddHours(2)),
            });

            var reader = new FileAuditTrailReader(path, NullLogger<FileAuditTrailReader>.Instance);
            var result = await reader.ReadAsync(new ViewAuditTrailRequest(), 10);

            Assert.Equal(3, result.Count);
            // Most recent (a3) must come first
            Assert.Equal("a3", result[0].AuditId);
            Assert.Equal("a1", result[2].AuditId);
        }

        [Fact]
        public async Task ReadAsync_FilterByAgentId_ReturnsOnlyMatching()
        {
            var path = TempLogPath();
            var ts   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            WriteLines(path, new[]
            {
                MakeRecord("b1", "agent-A", "PASS", ts),
                MakeRecord("b2", "agent-B", "PASS", ts.AddHours(1)),
                MakeRecord("b3", "agent-A", "FAIL", ts.AddHours(2)),
            });

            var reader = new FileAuditTrailReader(path, NullLogger<FileAuditTrailReader>.Instance);
            var result = await reader.ReadAsync(
                new ViewAuditTrailRequest { AgentId = "agent-A" }, 10);

            Assert.Equal(2, result.Count);
            Assert.All(result, e => Assert.Equal("agent-A", e.AgentId));
        }

        [Fact]
        public async Task ReadAsync_FilterByFinalStatus_ReturnsOnlyMatching()
        {
            var path = TempLogPath();
            var ts   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            WriteLines(path, new[]
            {
                MakeRecord("c1", "agent-1", "PASS",   ts),
                MakeRecord("c2", "agent-1", "FAIL",   ts.AddHours(1)),
                MakeRecord("c3", "agent-1", "REVIEW", ts.AddHours(2)),
                MakeRecord("c4", "agent-1", "FAIL",   ts.AddHours(3)),
            });

            var reader = new FileAuditTrailReader(path, NullLogger<FileAuditTrailReader>.Instance);
            var result = await reader.ReadAsync(
                new ViewAuditTrailRequest { FinalStatus = "FAIL" }, 10);

            Assert.Equal(2, result.Count);
            Assert.All(result, e => Assert.Equal("FAIL", e.FinalStatus));
        }

        [Fact]
        public async Task ReadAsync_FilterByDateRange_ReturnsOnlyInRange()
        {
            var path = TempLogPath();
            var day1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var day3 = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);
            var day5 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc);
            WriteLines(path, new[]
            {
                MakeRecord("d1", "agent-1", "PASS", day1),
                MakeRecord("d2", "agent-1", "PASS", day3),
                MakeRecord("d3", "agent-1", "PASS", day5),
            });

            var reader = new FileAuditTrailReader(path, NullLogger<FileAuditTrailReader>.Instance);
            var result = await reader.ReadAsync(
                new ViewAuditTrailRequest { From = day1.AddDays(1), To = day3.AddDays(1) }, 10);

            Assert.Single(result);
            Assert.Equal("d2", result[0].AuditId);
        }

        [Fact]
        public async Task ReadAsync_CorruptLine_SkipsAndContinues()
        {
            var path = TempLogPath();
            var ts   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            using (var sw = new StreamWriter(path))
            {
                sw.WriteLine(JsonSerializer.Serialize(MakeRecord("e1", "agent-1", "PASS", ts)));
                sw.WriteLine("{ this is not valid JSON !!! }");     // corrupt
                sw.WriteLine(JsonSerializer.Serialize(MakeRecord("e3", "agent-1", "FAIL", ts.AddHours(1))));
            }

            var reader = new FileAuditTrailReader(path, NullLogger<FileAuditTrailReader>.Instance);
            var result = await reader.ReadAsync(new ViewAuditTrailRequest(), 10);

            // 2 valid lines, 1 corrupt skipped
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task ReadAsync_RespectLimit()
        {
            var path = TempLogPath();
            var ts   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            WriteLines(path, new[]
            {
                MakeRecord("f1", "agent-1", "PASS", ts),
                MakeRecord("f2", "agent-1", "PASS", ts.AddHours(1)),
                MakeRecord("f3", "agent-1", "PASS", ts.AddHours(2)),
                MakeRecord("f4", "agent-1", "PASS", ts.AddHours(3)),
            });

            var reader = new FileAuditTrailReader(path, NullLogger<FileAuditTrailReader>.Instance);
            var result = await reader.ReadAsync(new ViewAuditTrailRequest(), limit: 2);

            Assert.Equal(2, result.Count);
        }
    }
}
