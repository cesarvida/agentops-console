using System;
using System.Collections.Generic;
using AgentOps.Core.ValueObjects;
using AgentOps.Core.Exceptions;

namespace AgentOps.Core.Entities
{
    public class AgentDefinition
    {
        public AgentId Id { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
        public string Purpose { get; init; }
        public List<string> Rules { get; init; }
        public List<string> Tools { get; init; }
        public AgentConfiguration Configuration { get; init; }
        public DateTime CreatedAt { get; init; }
        public string Version { get; init; }

        public AgentDefinition(
            AgentId id,
            string name,
            string description,
            string purpose,
            List<string> rules,
            List<string> tools,
            AgentConfiguration configuration,
            DateTime createdAt,
            string version)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length < 3 || name.Length > 100)
                throw new InvalidAgentDefinitionException("Name must be 3-100 characters.");
            if (string.IsNullOrWhiteSpace(description) || description.Length < 10)
                throw new InvalidAgentDefinitionException("Description must be at least 10 characters.");
            if (string.IsNullOrWhiteSpace(purpose))
                throw new InvalidAgentDefinitionException("Purpose is required.");
            if (rules == null || rules.Count == 0)
                throw new InvalidAgentDefinitionException("At least one rule is required.");
            if (tools == null || tools.Count == 0)
                throw new InvalidAgentDefinitionException("At least one tool is required.");
            Id = id;
            Name = name;
            Description = description;
            Purpose = purpose;
            Rules = rules;
            Tools = tools;
            Configuration = configuration;
            CreatedAt = createdAt;
            Version = version;
        }
    }

    public class AgentConfiguration
    {
        public int MaxTokensPerRequest { get; set; }
        public double TemperatureDefault { get; set; }
        public bool AllowHallucination { get; set; }
        public bool RequiresAudit { get; set; }
    }
}
