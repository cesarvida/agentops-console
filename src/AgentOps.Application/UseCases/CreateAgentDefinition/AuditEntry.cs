using System;

namespace AgentOps.Application.UseCases.CreateAgentDefinition
{
    public class AuditEntry
    {
        public DateTime TimestampUtc { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }
    }
}
