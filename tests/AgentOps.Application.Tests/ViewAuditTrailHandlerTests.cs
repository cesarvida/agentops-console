using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.ViewAuditTrail;
using Xunit;

namespace AgentOps.Application.Tests
{
    public class ViewAuditTrailHandlerTests
    {
        // ─── fake reader ───────────────────────────────────────────────────────

        private class InMemoryAuditTrailReader : IAuditTrailReader
        {
            private readonly List<AuditEntrySummary> _all;

            public InMemoryAuditTrailReader(IEnumerable<AuditEntrySummary> entries)
            {
                _all = entries.ToList();
            }

            public Task<IReadOnlyList<AuditEntrySummary>> ReadAsync(ViewAuditTrailRequest request, int limit)
            {
                // Return up to limit matching the request filters
                var matches = _all.Where(e =>
                    (string.IsNullOrWhiteSpace(request.AgentId)     || e.AgentId == request.AgentId) &&
                    (string.IsNullOrWhiteSpace(request.FinalStatus) || e.FinalStatus == request.FinalStatus))
                    .Take(limit)
                    .ToList();

                return Task.FromResult<IReadOnlyList<AuditEntrySummary>>(matches);
            }
        }

        private static AuditEntrySummary Entry(string id, string agentId, string status) =>
            new() { AuditId = id, AgentId = agentId, FinalStatus = status,
                    TimestampUtc = "2026-01-01T00:00:00Z", EvaluationId = $"eval-{id}",
                    ScenarioId = "prompt-injection-probe-v1", OverallRiskLevel = "Low",
                    CombinedQualityScore = 80 };

        // ─── tests ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task Handle_NoFilter_ReturnsAllUpToPageSize()
        {
            var reader  = new InMemoryAuditTrailReader(new[]
            {
                Entry("1", "agent-A", "PASS"),
                Entry("2", "agent-B", "FAIL"),
                Entry("3", "agent-A", "REVIEW"),
            });
            var handler = new ViewAuditTrailHandler(reader);

            var resp = await handler.HandleAsync(new ViewAuditTrailRequest { PageSize = 10 });

            Assert.Equal(3, resp.Entries.Count);
            Assert.False(resp.HasMore);
        }

        [Fact]
        public async Task Handle_HasMore_WhenMoreThanPageSize()
        {
            var entries = Enumerable.Range(1, 12)
                .Select(i => Entry(i.ToString(), "agent-A", "PASS"))
                .ToList();
            var reader  = new InMemoryAuditTrailReader(entries);
            var handler = new ViewAuditTrailHandler(reader);

            var resp = await handler.HandleAsync(new ViewAuditTrailRequest { PageSize = 10 });

            Assert.Equal(10, resp.Entries.Count);
            Assert.True(resp.HasMore);
        }

        [Fact]
        public async Task Handle_InvalidFinalStatus_Throws()
        {
            var reader  = new InMemoryAuditTrailReader(Array.Empty<AuditEntrySummary>());
            var handler = new ViewAuditTrailHandler(reader);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                handler.HandleAsync(new ViewAuditTrailRequest { FinalStatus = "UNKNOWN" }));
        }

        [Fact]
        public async Task Handle_FromGreaterThanTo_Throws()
        {
            var reader  = new InMemoryAuditTrailReader(Array.Empty<AuditEntrySummary>());
            var handler = new ViewAuditTrailHandler(reader);
            var req     = new ViewAuditTrailRequest
            {
                From = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                To   = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(req));
        }

        [Fact]
        public async Task Handle_InvalidPageSize_Throws()
        {
            var reader  = new InMemoryAuditTrailReader(Array.Empty<AuditEntrySummary>());
            var handler = new ViewAuditTrailHandler(reader);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                handler.HandleAsync(new ViewAuditTrailRequest { PageSize = 0 }));
        }

        [Fact]
        public async Task Handle_EmptyLog_ReturnsEmptyResponse()
        {
            var reader  = new InMemoryAuditTrailReader(Array.Empty<AuditEntrySummary>());
            var handler = new ViewAuditTrailHandler(reader);

            var resp = await handler.HandleAsync(new ViewAuditTrailRequest());

            Assert.Empty(resp.Entries);
            Assert.False(resp.HasMore);
        }
    }
}
