using System;
using System.IO;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Application.Governance
{
    /// <summary>
    /// Loads agent definitions from files in either YAML or JSON format.
    /// Automatically detects the file format based on extension (.yaml, .yml, .json)
    /// and delegates to the appropriate deserializer.
    /// </summary>
    public static class AgentDefinitionLoader
    {
        private static readonly AgentYamlDeserializer _yamlDeserializer = new();
        private static readonly AgentJsonDeserializer _jsonDeserializer = new();

        /// <summary>
        /// Loads an agent definition from a file path.
        /// Supports .yaml, .yml, and .json extensions.
        /// </summary>
        /// <param name="filePath">Path to the agent definition file.</param>
        /// <returns>The deserialized <see cref="AgentDefinition"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if filePath is null or empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
        /// <exception cref="NotSupportedException">Thrown if the file extension is not .yaml, .yml, or .json.</exception>
        /// <exception cref="InvalidOperationException">Thrown if deserialization fails.</exception>
        public static async Task<AgentDefinition> LoadAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Agent definition file not found: {filePath}");

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var content = await File.ReadAllTextAsync(filePath);

            return extension switch
            {
                ".yaml" or ".yml" => _yamlDeserializer.Deserialize(content),
                ".json" => _jsonDeserializer.Deserialize(content),
                _ => throw new NotSupportedException(
                    $"Unsupported agent definition file extension: '{extension}'. " +
                    $"Supported extensions are: .yaml, .yml, .json")
            };
        }

        /// <summary>
        /// Synchronous version of LoadAsync for convenience when async is not required.
        /// </summary>
        public static AgentDefinition Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Agent definition file not found: {filePath}");

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var content = File.ReadAllText(filePath);

            return extension switch
            {
                ".yaml" or ".yml" => _yamlDeserializer.Deserialize(content),
                ".json" => _jsonDeserializer.Deserialize(content),
                _ => throw new NotSupportedException(
                    $"Unsupported agent definition file extension: '{extension}'. " +
                    $"Supported extensions are: .yaml, .yml, .json")
            };
        }
    }
}
