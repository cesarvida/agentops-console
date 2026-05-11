using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentOps.Infrastructure.Persistence
{
    public class YamlEvaluationScenarioRepository : IEvaluationScenarioRepository
    {
        private readonly string _yamlPath;
        private readonly Dictionary<string, EvaluationScenario> _cache = new();

        public YamlEvaluationScenarioRepository()
        {
            // Try multiple locations to find evaluation-scenarios.mcp.yaml
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "evaluation-scenarios.mcp.yaml"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "AgentOps.Infrastructure", "Resources", "evaluation-scenarios.mcp.yaml"),
                Path.Combine(Directory.GetCurrentDirectory(), "evaluation-scenarios.mcp.yaml"),
                "evaluation-scenarios.mcp.yaml"
            };

            _yamlPath = candidates.FirstOrDefault(p => File.Exists(p)) ?? candidates[0];
            Load();
        }

        private void Load()
        {
            if (!File.Exists(_yamlPath))
            {
                Console.WriteLine($"[WARN] EvaluationScenario file not found at: {_yamlPath}");
                return;
            }

            var text = File.ReadAllText(_yamlPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            try
            {
                var doc = deserializer.Deserialize<ScenarioDoc>(text);
                if (doc?.Scenarios != null)
                {
                    foreach (var s in doc.Scenarios)
                    {
                        _cache[s.ScenarioId] = s;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to parse EvaluationScenario YAML: {ex.Message}");
            }
        }

        public Task<EvaluationScenario?> GetByIdAsync(string scenarioId)
        {
            _cache.TryGetValue(scenarioId, out var s);
            return Task.FromResult(s);
        }

        private class ScenarioDoc
        {
            public List<EvaluationScenario>? Scenarios { get; set; }
        }
    }
}
