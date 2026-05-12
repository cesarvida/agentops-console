using System;
using System.IO;
using System.Threading.Tasks;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.ValueObjects;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

            return report;
        }

        /// <summary>
        /// Deserializes a YAML string into an AgentDefinition.
        /// </summary>
        private AgentDefinition DeserializeAgentDefinition(string yaml)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                // This is a simplified deserialization - in real usage, you'd have
                // proper YAML structure matching AgentDefinition fields
                var obj = deserializer.Deserialize<dynamic>(yaml);

                // Extract basic fields
                string id = obj?["id"] ?? obj?["agent"]?["id"] ?? Guid.NewGuid().ToString();
                string name = obj?["name"] ?? obj?["agent"]?["name"] ?? "Unknown";
                string version = obj?["version"] ?? obj?["agent"]?["version"] ?? "0.0.0";
                string description = obj?["description"] ?? obj?["agent"]?["description"] ?? "";

                var config = new AgentConfiguration
                {
                    Owner = obj?["owner"] ?? obj?["agent"]?["owner"] ?? "",
                    RequiresAudit = true
                };

                // Extract allowed actions if present
                var allowedActionsObj = obj?["allowedActions"] ?? obj?["governance"]?["allowed_actions"];
                if (allowedActionsObj != null)
                {
                    // Handle different YAML structures
                    if (allowedActionsObj is System.Collections.IEnumerable enumerable)
                    {
                        foreach (var action in enumerable)
                        {
                            config.AllowedActions.Add(action?.ToString() ?? "");
                        }
                    }
                }

                return new AgentDefinition(
                    new AgentId(id),
                    name,
                    description,
                    purpose: "Validated from YAML",
                    rules: new System.Collections.Generic.List<string>(),
                    tools: new System.Collections.Generic.List<string>(),
                    configuration: config,
                    createdAt: DateTime.UtcNow,
                    version: version
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deserializing agent YAML: {ex.Message}", ex);
            }
        }
    }
}
