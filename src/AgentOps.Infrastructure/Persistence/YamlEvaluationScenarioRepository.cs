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
            // Get the directory where this assembly (Infrastructure) is located
            var infrastructureDir = Path.GetDirectoryName(typeof(YamlEvaluationScenarioRepository).Assembly.Location);
            
            // Build candidate paths for evaluation-scenarios.mcp.yaml
            var candidates = new List<string>();
            
            if (!string.IsNullOrEmpty(infrastructureDir))
            {
                // 1. Same directory as Infrastructure assembly (if copied there)
                candidates.Add(Path.Combine(infrastructureDir, "evaluation-scenarios.mcp.yaml"));
                
                // 2. In a Resources subdirectory relative to Infrastructure assembly
                candidates.Add(Path.Combine(infrastructureDir, "Resources", "evaluation-scenarios.mcp.yaml"));
                
                // 3. Walk up to find source tree: bin/Release/net10.0 -> src/AgentOps.Infrastructure/Resources
                var parts = infrastructureDir.Split(Path.DirectorySeparatorChar);
                if (parts.Length > 3 && parts[^3] == "bin")
                {
                    // We're in bin/Release/net10.0, walk up to src
                    var basePath = string.Join(Path.DirectorySeparatorChar, parts.Take(parts.Length - 3));
                    candidates.Add(Path.Combine(basePath, "AgentOps.Infrastructure", "Resources", "evaluation-scenarios.mcp.yaml"));
                }
            }
            
            // 4. Current working directory (for running from project root)
            candidates.Add("evaluation-scenarios.mcp.yaml");
            candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "evaluation-scenarios.mcp.yaml"));
            candidates.Add(Path.Combine(AppContext.BaseDirectory, "evaluation-scenarios.mcp.yaml"));

            _yamlPath = null;
            foreach (var candidate in candidates)
            {
                try
                {
                    var normalized = Path.GetFullPath(candidate);
                    if (File.Exists(normalized))
                    {
                        _yamlPath = normalized;
                        break;
                    }
                }
                catch
                {
                    // Skip invalid paths
                }
            }

            if (_yamlPath == null)
            {
                // Fallback to first candidate for error reporting
                _yamlPath = candidates.FirstOrDefault() ?? "evaluation-scenarios.mcp.yaml";
                Console.WriteLine($"[WARN] EvaluationScenario YAML file not found. Candidates:");
                foreach (var c in candidates)
                {
                    try
                    {
                        var full = Path.GetFullPath(c);
                        Console.WriteLine($"       {full}");
                    }
                    catch
                    {
                        Console.WriteLine($"       {c}");
                    }
                }
            }

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
