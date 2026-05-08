using System;

namespace AgentOps.Application.UseCases.CreateAgentDefinition
{
    public class CreateAgentDefinitionResponse
    {
        public string AgentId { get; set; }
        public string SavedPath { get; set; }
        public string AuditId { get; set; }
        public string Status { get; set; }
        public string? Errors { get; set; }
        public string? Warnings { get; set; }
    }
}
