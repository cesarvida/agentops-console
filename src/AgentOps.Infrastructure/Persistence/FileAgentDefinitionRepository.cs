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
                    var agent = ParseAgentDefinition(json);
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
                var agent = ParseAgentDefinition(json);
                return agent;
            }
            catch
            {
                return null;
            }
        }

        // Parse the on-disk JSON into a runtime AgentDefinition instance.
        // This handles the saved shape where Id may be an object { "Value": "..." }.
        private AgentDefinition? ParseAgentDefinition(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string idVal = string.Empty;
                if (root.TryGetProperty("Id", out var idEl))
                {
                    if (idEl.ValueKind == JsonValueKind.String)
                    {
                        idVal = idEl.GetString() ?? string.Empty;
                    }
                    else if (idEl.ValueKind == JsonValueKind.Object && idEl.TryGetProperty("Value", out var v) && v.ValueKind == JsonValueKind.String)
                    {
                        idVal = v.GetString() ?? string.Empty;
                    }
                }

                var name = root.TryGetProperty("Name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() ?? string.Empty : string.Empty;
                var description = root.TryGetProperty("Description", out var d) && d.ValueKind == JsonValueKind.String ? d.GetString() ?? string.Empty : string.Empty;
                var purpose = root.TryGetProperty("Purpose", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() ?? string.Empty : string.Empty;

                var rules = new List<string>();
                if (root.TryGetProperty("Rules", out var rulesEl) && rulesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in rulesEl.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String) rules.Add(item.GetString()!);
                }

                var tools = new List<string>();
                if (root.TryGetProperty("Tools", out var toolsEl) && toolsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in toolsEl.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String) tools.Add(item.GetString()!);
                }

                var config = new AgentConfiguration();
                if (root.TryGetProperty("Configuration", out var confEl) && confEl.ValueKind == JsonValueKind.Object)
                {
                    if (confEl.TryGetProperty("MaxTokensPerRequest", out var mt) && mt.ValueKind == JsonValueKind.Number) config.MaxTokensPerRequest = mt.GetInt32();
                    if (confEl.TryGetProperty("TemperatureDefault", out var td) && td.ValueKind == JsonValueKind.Number) config.TemperatureDefault = td.GetDouble();
                    if (confEl.TryGetProperty("AllowHallucination", out var ah) && ah.ValueKind == JsonValueKind.True) config.AllowHallucination = true;
                    if (confEl.TryGetProperty("RequiresAudit", out var ra) && ra.ValueKind == JsonValueKind.True) config.RequiresAudit = true;
                }

                DateTime createdAt = DateTime.UtcNow;
                if (root.TryGetProperty("CreatedAt", out var ca) && ca.ValueKind == JsonValueKind.String)
                {
                    if (!DateTime.TryParse(ca.GetString(), out createdAt)) createdAt = DateTime.UtcNow;
                }

                var version = root.TryGetProperty("Version", out var ver) && ver.ValueKind == JsonValueKind.String ? ver.GetString() ?? "1.0" : "1.0";

                if (string.IsNullOrWhiteSpace(idVal)) return null;

                var agent = new AgentDefinition(
                    new AgentId(idVal),
                    name,
                    description,
                    purpose,
                    rules,
                    tools,
                    config,
                    createdAt,
                    version
                );

                return agent;
            }
            catch
            {
                return null;
            }
        }
    }
}
