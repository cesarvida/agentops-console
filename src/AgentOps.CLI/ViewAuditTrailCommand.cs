using System;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.ViewAuditTrail;

namespace AgentOps.CLI
{
    public class ViewAuditTrailCommand
    {
        private const int PageSize = 10;

        private readonly ViewAuditTrailHandler _handler;
        private readonly IConsoleWriter _console;

        public ViewAuditTrailCommand(ViewAuditTrailHandler handler, IConsoleWriter console)
        {
            _handler = handler;
            _console = console;
        }

        public async Task ExecuteAsync()
        {
            _console.WriteLine("=== View Audit Trail ===");
            _console.WriteLine("Filter by AgentId       (Enter to skip): ");
            var agentId = Console.ReadLine()?.Trim();

            _console.WriteLine("Filter by FinalStatus   PASS/REVIEW/FAIL (Enter to skip): ");
            var status = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(status)) status = null;

            _console.WriteLine("Filter from date        yyyy-MM-dd UTC (Enter to skip): ");
            var fromStr = Console.ReadLine()?.Trim();
            DateTime? from = TryParseDate(fromStr);

            _console.WriteLine("Filter to date          yyyy-MM-dd UTC (Enter to skip): ");
            var toStr = Console.ReadLine()?.Trim();
            DateTime? to = TryParseDate(toStr);

            var req = new ViewAuditTrailRequest
            {
                AgentId     = string.IsNullOrWhiteSpace(agentId) ? null : agentId,
                FinalStatus = status,
                From        = from,
                To          = to.HasValue ? to.Value.AddDays(1).AddSeconds(-1) : null, // inclusive end
                PageSize    = PageSize
            };

            var offset = 0;
            while (true)
            {
                ViewAuditTrailResponse resp;
                try
                {
                    resp = await _handler.HandleAsync(new ViewAuditTrailRequest
                {
                    AgentId     = req.AgentId,
                    FinalStatus = req.FinalStatus,
                    From        = req.From,
                    To          = req.To,
                    PageSize    = PageSize
                });
                }
                catch (ArgumentException ex)
                {
                    _console.WriteWarning($"Invalid filter: {ex.Message}");
                    return;
                }

                if (resp.Entries.Count == 0)
                {
                    if (offset == 0)
                        _console.WriteLine("No audit entries found.");
                    else
                        _console.WriteLine("No more entries.");
                    return;
                }

                // Header
                _console.WriteLine("");
                _console.WriteLine($"{"Timestamp (UTC)",-26} {"AgentId",-14} {"Scenario",-35} {"Status",-8} {"Risk",-8} {"Score",5} {"Findings",8}");
                _console.WriteLine(new string('─', 110));

                foreach (var e in resp.Entries)
                {
                    var agShort  = Truncate(e.AgentId,      14);
                    var scShort  = Truncate(e.ScenarioId,   35);
                    var tsShort  = Truncate(e.TimestampUtc, 26);
                    _console.WriteLine(
                        $"{tsShort,-26} {agShort,-14} {scShort,-35} {e.FinalStatus,-8} {e.OverallRiskLevel,-8} {e.CombinedQualityScore,5} {e.FindingsCount,8}");
                }

                offset += resp.Entries.Count;

                if (!resp.HasMore) return;

                _console.WriteLine("");
                _console.WriteLine("Show more? (y/n): ");
                var more = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (more != "y") return;

                // Shift window: ask handler for next page by adjusting To boundary
                // Simple approach: re-read with same filters — reader already iterates in reverse,
                // so we pass the last seen timestamp as the new upper bound (exclusive).
                var lastTs = resp.Entries[resp.Entries.Count - 1].TimestampUtc;
                if (!DateTime.TryParse(lastTs, out var lastDt)) break;
                req = new ViewAuditTrailRequest
                {
                    AgentId     = req.AgentId,
                    FinalStatus = req.FinalStatus,
                    From        = req.From,
                    To          = lastDt.AddTicks(-1),
                    PageSize    = req.PageSize
                };
            }
        }

        private static DateTime? TryParseDate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            if (DateTime.TryParse(input, out var dt))
                return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            return null;
        }

        private static string Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s[..(max - 1)] + "…";
        }
    }
}
