using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.Governance;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Entities;
using AgentOps.GitHub;

namespace AgentOps.Infrastructure.GitHub
{
    /// <summary>
    /// Fetches agent YAML definitions from a GitHub repository's
    /// <c>data/agent-definitions/</c> directory using the GitHub Contents API.
    /// </summary>
    public class GitHubAgentFetcher : IAgentFetcher
    {
        private const string AgentDefinitionsPath = "data/agent-definitions";

        private readonly GitHubHttpClient _httpClient;
        private readonly AgentYamlDeserializer _deserializer;

        public GitHubAgentFetcher(GitHubHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _deserializer = new AgentYamlDeserializer();
        }

        /// <inheritdoc />
        public async Task<List<AgentDefinition>> FetchAgentsAsync(string owner, string repo)
        {
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException("Owner cannot be empty.", nameof(owner));
            if (string.IsNullOrWhiteSpace(repo))
                throw new ArgumentException("Repo cannot be empty.", nameof(repo));

            var agents = new List<AgentDefinition>();

            // 1. List files in data/agent-definitions/
            string listJson;
            try
            {
                listJson = await _httpClient.GetAsync($"/repos/{owner}/{repo}/contents/{AgentDefinitionsPath}");
            }
            catch (Exception ex) when (IsNotFound(ex))
            {
                // Directory doesn't exist in this repo — return empty list gracefully
                Console.WriteLine($"[INFO] No agent definitions directory found in {owner}/{repo}: {ex.Message}");
                return agents;
            }

            List<GitHubContentItem>? items;
            try
            {
                items = JsonSerializer.Deserialize<List<GitHubContentItem>>(listJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[WARN] Could not parse directory listing from GitHub API: {ex.Message}");
                return agents;
            }

            if (items == null || items.Count == 0)
                return agents;

            // 2. For each .yaml file, download and deserialize
            foreach (var item in items)
            {
                if (item.Type != "file" || !item.Name.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    var fileJson = await _httpClient.GetAsync($"/repos/{owner}/{repo}/contents/{item.Path}");
                    var fileContent = JsonSerializer.Deserialize<GitHubFileContent>(fileJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (fileContent == null || string.IsNullOrWhiteSpace(fileContent.Content))
                        continue;

                    // GitHub returns content as base64 with newlines
                    var yamlBytes = Convert.FromBase64String(fileContent.Content.Replace("\n", ""));
                    var yaml = Encoding.UTF8.GetString(yamlBytes);

                    var agent = _deserializer.Deserialize(yaml);
                    agents.Add(agent);
                }
                catch (Exception ex)
                {
                    // Skip files that fail to parse — log and continue
                    Console.WriteLine($"[WARN] Skipping '{item.Name}': {ex.Message}");
                }
            }

            return agents;
        }

        /// <summary>
        /// Returns true if the exception indicates an HTTP 404 Not Found.
        /// </summary>
        private static bool IsNotFound(Exception ex)
            => ex.Message.Contains("404") || ex.Message.Contains("Not Found", StringComparison.OrdinalIgnoreCase);

        // ── Private DTOs for GitHub Contents API ──────────────────────────────

        private sealed class GitHubContentItem
        {
            public string Name { get; set; } = string.Empty;
            public string Path { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;   // "file" | "dir"
        }

        private sealed class GitHubFileContent
        {
            public string Content  { get; set; } = string.Empty;   // base64-encoded
            public string Encoding { get; set; } = string.Empty;   // "base64"
        }
    }
}
