using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.CreateAgentDefinition;

namespace AgentOps.Infrastructure.Persistence
{
    public class FileAuditRepository : IAuditRepository
    {
        private readonly string _auditPath;
        public FileAuditRepository(string auditPath)
        {
            _auditPath = auditPath;
            var dir = Path.GetDirectoryName(_auditPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }

        public async Task<string> AppendAsync(AuditEntry entry)
        {
            var auditId = Guid.NewGuid().ToString();
            var record = new AuditLogRecord
            {
                AuditId = auditId,
                TimestampUtc = entry.TimestampUtc,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                Status = entry.Status,
                Details = entry.Details
            };
            var json = JsonSerializer.Serialize(record) + Environment.NewLine;
            await File.AppendAllTextAsync(_auditPath, json);
            return auditId;
        }

        private class AuditLogRecord
        {
            public string AuditId { get; set; }
            public DateTime TimestampUtc { get; set; }
            public string Action { get; set; }
            public string EntityType { get; set; }
            public string EntityId { get; set; }
            public string Status { get; set; }
            public string Details { get; set; }
        }
    }
}
