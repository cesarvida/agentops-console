using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Governance;
using AgentOps.GitHub;

namespace AgentOps.Infrastructure.Config
{
    public class GovernanceConfigLoader : IGovernanceConfigLoader
    {
        private const string ConfigPath = "data/governance-config.yaml";
        private readonly GitHubHttpClient _httpClient;

        public GovernanceConfigLoader(GitHubHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<GovernanceConfig> LoadAsync(string owner, string repo)
        {
            try
            {
                var json = await _httpClient.GetAsync(
                    $"/repos/{owner}/{repo}/contents/{ConfigPath}");

                var file = JsonSerializer.Deserialize<GitHubFile>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (file == null || string.IsNullOrWhiteSpace(file.Content))
                    return GovernanceConfig.Default;

                var yamlBytes = Convert.FromBase64String(file.Content.Replace("\n", ""));
                var yaml      = Encoding.UTF8.GetString(yamlBytes);

                return GovernanceConfigParser.Parse(yaml);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[INFO] Could not load governance-config.yaml from {owner}/{repo}: {ex.Message}. Using defaults.");
                return GovernanceConfig.Default;
            }
        }

        private sealed class GitHubFile
        {
            public string Content  { get; set; } = string.Empty;
            public string Encoding { get; set; } = string.Empty;
        }
    }
}
