using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentOps.Application.UseCases.ViewAuditTrail
{
    public interface IAuditTrailReader
    {
        /// <summary>
        /// Reads audit summaries in reverse-chronological order (most recent first),
        /// applying optional filters. Returns at most <paramref name="limit"/> + 1 items
        /// so the caller can detect whether more entries exist.
        /// </summary>
        Task<IReadOnlyList<AuditEntrySummary>> ReadAsync(ViewAuditTrailRequest request, int limit);
    }
}
