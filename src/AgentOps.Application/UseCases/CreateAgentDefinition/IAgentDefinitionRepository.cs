using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;

namespace AgentOps.Application.UseCases.CreateAgentDefinition
{
    public interface IAgentDefinitionRepository
    {
        Task<string> AddAsync(AgentDefinition agentDefinition);
        // List all persisted agent definitions (MVP simple)
        Task<IEnumerable<AgentDefinition>> ListAllAsync();
        // Get a single AgentDefinition by id
        Task<AgentDefinition?> GetByIdAsync(AgentId id);
        // Optionally: Task<AgentDefinition?> GetByIdAsync(AgentId id);
    }
}
