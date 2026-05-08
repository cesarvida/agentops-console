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
            // Expect file copied to output root
            _yamlPath = Path.Combine(AppContext.BaseDirectory, "evaluation-scenarios.mcp.yaml");
            Load();
        }

        private void Load()
        {
            if (!File.Exists(_yamlPath)) return;
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
            catch
            {
                // ignore parse errors; repository will be empty
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
