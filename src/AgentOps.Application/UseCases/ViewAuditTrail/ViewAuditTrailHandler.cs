using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentOps.Application.UseCases.ViewAuditTrail
{
    public class ViewAuditTrailHandler
    {
        private readonly IAuditTrailReader _reader;

        public ViewAuditTrailHandler(IAuditTrailReader reader)
        {
            _reader = reader;
        }

        public async Task<ViewAuditTrailResponse> HandleAsync(ViewAuditTrailRequest req)
        {
            // Validate
            if (req.PageSize <= 0) throw new ArgumentException("PageSize must be > 0.");
            if (!string.IsNullOrWhiteSpace(req.FinalStatus) &&
                req.FinalStatus != "PASS" && req.FinalStatus != "REVIEW" && req.FinalStatus != "FAIL")
                throw new ArgumentException("FinalStatus must be PASS, REVIEW, or FAIL.");
            if (req.From.HasValue && req.To.HasValue && req.From > req.To)
                throw new ArgumentException("From must be <= To.");

            // Fetch one extra to detect HasMore without loading everything in memory
            var raw = await _reader.ReadAsync(req, req.PageSize + 1);

            var hasMore = raw.Count > req.PageSize;
            var page    = hasMore ? raw.Take(req.PageSize).ToList() : raw.ToList();

            return new ViewAuditTrailResponse
            {
                Entries = page,
                HasMore = hasMore
            };
        }
    }
}
