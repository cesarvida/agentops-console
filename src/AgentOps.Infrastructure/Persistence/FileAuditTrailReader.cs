using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Application.UseCases.ViewAuditTrail;
using Microsoft.Extensions.Logging;

namespace AgentOps.Infrastructure.Persistence
{
    /// <summary>
    /// Reads MetricsSummary entries from the append-only audit.log (JSON Lines).
    /// Lines are iterated in reverse so most-recent entries appear first.
    /// Corrupted lines are skipped with a warning — they never crash the reader.
    /// </summary>
    public class FileAuditTrailReader : IAuditTrailReader
    {
        private readonly string _auditPath;
        private readonly ILogger<FileAuditTrailReader> _logger;

        public FileAuditTrailReader(string auditPath, ILogger<FileAuditTrailReader> logger)
        {
            _auditPath = auditPath;
            _logger    = logger;
        }

        public Task<IReadOnlyList<AuditEntrySummary>> ReadAsync(ViewAuditTrailRequest request, int limit)
        {
            var results = new List<AuditEntrySummary>();

            if (!File.Exists(_auditPath))
                return Task.FromResult<IReadOnlyList<AuditEntrySummary>>(results);

            // Read all lines up front (file is text; for very large files a streaming
            // reverse-reader would be preferred, but for governance audit logs that
            // rarely exceed tens of thousands of lines this is acceptable and safe).
            string[] lines;
            try
            {
                lines = File.ReadAllLines(_auditPath, Encoding.UTF8);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Could not read audit log at {Path}", _auditPath);
                return Task.FromResult<IReadOnlyList<AuditEntrySummary>>(results);
            }

            // Iterate in reverse (most recent first)
            for (var i = lines.Length - 1; i >= 0 && results.Count < limit; i--)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                AuditLogRecord? record = null;
                try
                {
                    record = JsonSerializer.Deserialize<AuditLogRecord>(line);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Skipping corrupt audit line {Index}", i);
                    continue;
                }

                if (record == null) continue;

                // Only EvaluateAgentBehavior entries carry a MetricsSummary in Details
                MetricsSummary? metrics = null;
                if (!string.IsNullOrWhiteSpace(record.Details))
                {
                    try
                    {
                        metrics = JsonSerializer.Deserialize<MetricsSummary>(record.Details);
                    }
                    catch (JsonException)
                    {
                        // Details may be a plain-text message for non-evaluation entries
                        metrics = null;
                    }
                }

                var summary = new AuditEntrySummary
                {
                    AuditId            = record.AuditId      ?? string.Empty,
                    TimestampUtc       = record.TimestampUtc.ToString("o"),
                    Action             = record.Action        ?? string.Empty,
                    AgentId            = metrics?.AgentId     ?? string.Empty,
                    EvaluationId       = metrics?.EvaluationId ?? string.Empty,
                    ScenarioId         = record.EntityId      ?? string.Empty,
                    FinalStatus        = metrics?.FinalStatus  ?? record.Status ?? string.Empty,
                    OverallRiskLevel   = metrics?.OverallRiskLevel ?? string.Empty,
                    CombinedQualityScore  = metrics?.CombinedQualityScore  ?? 0,
                    CriticalFindingsCount = metrics?.CriticalFindingsCount ?? 0,
                    FindingsCount         = metrics?.FindingsCount         ?? 0,
                };

                // Apply filters
                if (!Matches(summary, record.TimestampUtc, request)) continue;

                results.Add(summary);
            }

            return Task.FromResult<IReadOnlyList<AuditEntrySummary>>(results);
        }

        private static bool Matches(AuditEntrySummary s, DateTime ts, ViewAuditTrailRequest req)
        {
            if (!string.IsNullOrWhiteSpace(req.AgentId) &&
                !string.Equals(s.AgentId, req.AgentId, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.IsNullOrWhiteSpace(req.FinalStatus) &&
                !string.Equals(s.FinalStatus, req.FinalStatus, StringComparison.OrdinalIgnoreCase))
                return false;

            if (req.From.HasValue && ts < req.From.Value) return false;
            if (req.To.HasValue   && ts > req.To.Value)   return false;

            return true;
        }

        // Mirrors FileAuditRepository.AuditLogRecord — private to avoid coupling
        private sealed class AuditLogRecord
        {
            public string?   AuditId      { get; set; }
            public DateTime  TimestampUtc { get; set; }
            public string?   Action       { get; set; }
            public string?   EntityType   { get; set; }
            public string?   EntityId     { get; set; }
            public string?   Status       { get; set; }
            public string?   Details      { get; set; }
        }
    }
}
