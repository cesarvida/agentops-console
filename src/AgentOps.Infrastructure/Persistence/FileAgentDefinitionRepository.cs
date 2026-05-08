using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using AgentOps.Application.UseCases.CreateAgentDefinition;

namespace AgentOps.Infrastructure.Persistence
{
    public class FileAgentDefinitionRepository : IAgentDefinitionRepository
    {
        private readonly string _basePath;
        public FileAgentDefinitionRepository(string basePath)
        {
            _basePath = basePath;
            Directory.CreateDirectory(_basePath);
        }

        public async Task<string> AddAsync(AgentDefinition agentDefinition)
        {
            var fileName = $"{agentDefinition.Id.Value}.json";
            var filePath = Path.Combine(_basePath, fileName);
            var json = JsonSerializer.Serialize(agentDefinition, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
            return filePath;
        }

        public async Task<IEnumerable<AgentDefinition>> ListAllAsync()
        {
            var list = new List<AgentDefinition>();
            if (!Directory.Exists(_basePath)) return list;

            var files = Directory.GetFiles(_basePath, "*.json");
            foreach (var f in files.OrderBy(x => x))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(f);
                    var agent = JsonSerializer.Deserialize<AgentDefinition>(json);
                    if (agent != null) list.Add(agent);
                }
                catch
                {
                    // skip malformed
                }
            }

            return list;
        }

        public async Task<AgentDefinition?> GetByIdAsync(AgentId id)
        {
            var fileName = $"{id.Value}.json";
            var filePath = Path.Combine(_basePath, fileName);
            if (!File.Exists(filePath)) return null;

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var agent = JsonSerializer.Deserialize<AgentDefinition>(json);
                return agent;
            }
            catch
            {
                return null;
            }
        }
    }
}
