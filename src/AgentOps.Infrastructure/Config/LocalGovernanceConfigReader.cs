using System;
using System.IO;
using AgentOps.Core.Governance;

namespace AgentOps.Infrastructure.Config
{
    /// <summary>
    /// Reads a <see cref="GovernanceConfig"/> from a local file path.
    /// Used by the CLI when running <c>validate-agent</c> without a GitHub API call.
    /// Falls back to <see cref="GovernanceConfig.Default"/> when the file is absent or unparseable.
    /// </summary>
    public static class LocalGovernanceConfigReader
    {
        private const string DefaultPath = "data/governance-config.yaml";

        /// <summary>
        /// Tries to load the governance config from the given file path.
        /// Returns <see cref="GovernanceConfig.Default"/> on any error.
        /// </summary>
        public static GovernanceConfig TryLoad(string filePath = DefaultPath)
        {
            if (!File.Exists(filePath))
                return GovernanceConfig.Default;

            try
            {
                var yaml = File.ReadAllText(filePath);
                return GovernanceConfigParser.Parse(yaml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Could not read local governance-config.yaml: {ex.Message}. Using defaults.");
                return GovernanceConfig.Default;
            }
        }
    }
}
