using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.ValueObjects;

namespace AgentOps.Application.Governance
{
    /// <summary>
    /// CQRS Command to validate an agent definition from a YAML file.
    /// </summary>
    public class ValidateAgentCommand
    {
        /// <summary>Path to the YAML file containing the agent definition.</summary>
        public string YamlPath { get; set; }

        public ValidateAgentCommand(string yamlPath)
        {
            YamlPath = yamlPath ?? throw new ArgumentNullException(nameof(yamlPath));
        }
    }

    /// <summary>
    /// Handler for ValidateAgentCommand that orchestrates governance evaluation.
    /// </summary>
    public class ValidateAgentCommandHandler
    {
        private readonly GovernanceRuleEngine _engine;
        private readonly AgentYamlDeserializer _yamlDeserializer = new();

        public ValidateAgentCommandHandler(GovernanceRuleEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        /// <summary>
        /// Executes the validation of an agent definition from a YAML file.
        /// </summary>
        /// <param name="command">The validation command with YAML path.</param>
        /// <returns>A governance report with evaluation results.</returns>
        public async Task<GovernanceReport> HandleAsync(ValidateAgentCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (!File.Exists(command.YamlPath))
            {
                throw new FileNotFoundException($"Agent YAML file not found: {command.YamlPath}");
            }

            // Read and parse YAML
            var yaml = await File.ReadAllTextAsync(command.YamlPath);
            var agent = DeserializeAgentDefinition(yaml);

            if (agent == null)
            {
                throw new InvalidOperationException("Failed to deserialize agent definition from YAML");
            }

            // Run governance evaluation
            var report = await _engine.EvaluateAsync(agent);

            // Persist JSON report to outputs/ directory
            await SaveReportAsJsonAsync(report);

            return report;
        }

        /// <summary>
        /// Saves the governance report as a JSON file in the outputs/ directory.
        /// </summary>
        private async Task SaveReportAsJsonAsync(GovernanceReport report)
        {
            try
            {
                const string outputDir = "outputs";
                Directory.CreateDirectory(outputDir);

                string safeId = string.IsNullOrWhiteSpace(report.AgentId)
                    ? "unknown"
                    : string.Concat(report.AgentId.Split(Path.GetInvalidFileNameChars()));
                string timestamp = report.EvaluatedAt.ToString("yyyyMMddHHmmss");
                string fileName = $"governance-{safeId}-{timestamp}.json";
                string filePath = Path.Combine(outputDir, fileName);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                string json = JsonSerializer.Serialize(report, options);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                // Non-critical — don't fail the evaluation if saving the report fails
                Console.WriteLine($"[WARN] Failed to save governance report JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Deserializes a YAML string into an AgentDefinition.
        /// Delegates to the shared <see cref="AgentYamlDeserializer"/>.
        /// </summary>
        private AgentDefinition DeserializeAgentDefinition(string yaml)
            => _yamlDeserializer.Deserialize(yaml);
    }
}
