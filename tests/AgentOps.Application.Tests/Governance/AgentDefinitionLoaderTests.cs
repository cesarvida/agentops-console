using System;
using System.IO;
using System.Threading.Tasks;
using AgentOps.Application.Governance;
using AgentOps.Core.Entities;
using Xunit;

namespace AgentOps.Application.Tests.Governance
{
    /// <summary>
    /// Tests for <see cref="AgentDefinitionLoader"/> to verify YAML and JSON loading,
    /// format equivalence, and governance behavior consistency.
    /// </summary>
    public class AgentDefinitionLoaderTests
    {
        private const string YamlApprovedAgent = @"
id: test-approved-001
name: Test Approved Agent
version: 1.0.0
description: Fully compliant test agent for JSON/YAML equivalence
purpose: Testing governance with both formats
owner: team-test
actions:
  - read_code
  - post_comment
rate_limit:
  requests_per_minute: 100
timeout_seconds: 60
environments:
  - development
audit:
  log_all_actions: true
  retention_days: 30
rules:
  - test_rule_1
  - test_rule_2
tools:
  - test_tool_1
  - test_tool_2
";

        private const string JsonApprovedAgent = @"{
  ""id"": ""test-approved-001"",
  ""name"": ""Test Approved Agent"",
  ""version"": ""1.0.0"",
  ""description"": ""Fully compliant test agent for JSON/YAML equivalence"",
  ""purpose"": ""Testing governance with both formats"",
  ""owner"": ""team-test"",
  ""actions"": [
    ""read_code"",
    ""post_comment""
  ],
  ""rate_limit"": {
    ""requests_per_minute"": 100
  },
  ""timeout_seconds"": 60,
  ""environments"": [
    ""development""
  ],
  ""audit"": {
    ""log_all_actions"": true,
    ""retention_days"": 30
  },
  ""rules"": [
    ""test_rule_1"",
    ""test_rule_2""
  ],
  ""tools"": [
    ""test_tool_1"",
    ""test_tool_2""
  ]
}";

        [Fact]
        public async Task LoadAsync_YamlFile_ReturnsValidAgentDefinition()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test-agent-{Guid.NewGuid():N}.yaml");
            try
            {
                await File.WriteAllTextAsync(tempFile, YamlApprovedAgent);

                // Act
                var agent = await AgentDefinitionLoader.LoadAsync(tempFile);

                // Assert
                Assert.NotNull(agent);
                Assert.Equal("test-approved-001", agent.Id.Value);
                Assert.Equal("Test Approved Agent", agent.Name);
                Assert.Equal("1.0.0", agent.Version);
                Assert.Equal("team-test", agent.Configuration.Owner);
                Assert.Equal(100, agent.Configuration.RateLimitRequestsPerMinute);
                Assert.Equal(60, agent.Configuration.TimeoutSeconds);
                Assert.Contains("development", agent.Configuration.Environments);
                Assert.Contains("read_code", agent.Configuration.AllowedActions);
                Assert.True(agent.Configuration.RequiresAudit);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadAsync_JsonFile_ReturnsValidAgentDefinition()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test-agent-{Guid.NewGuid():N}.json");
            try
            {
                await File.WriteAllTextAsync(tempFile, JsonApprovedAgent);

                // Act
                var agent = await AgentDefinitionLoader.LoadAsync(tempFile);

                // Assert
                Assert.NotNull(agent);
                Assert.Equal("test-approved-001", agent.Id.Value);
                Assert.Equal("Test Approved Agent", agent.Name);
                Assert.Equal("1.0.0", agent.Version);
                Assert.Equal("team-test", agent.Configuration.Owner);
                Assert.Equal(100, agent.Configuration.RateLimitRequestsPerMinute);
                Assert.Equal(60, agent.Configuration.TimeoutSeconds);
                Assert.Contains("development", agent.Configuration.Environments);
                Assert.Contains("read_code", agent.Configuration.AllowedActions);
                Assert.True(agent.Configuration.RequiresAudit);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadAsync_YmlExtension_ReturnsValidAgentDefinition()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test-agent-{Guid.NewGuid():N}.yml");
            try
            {
                await File.WriteAllTextAsync(tempFile, YamlApprovedAgent);

                // Act
                var agent = await AgentDefinitionLoader.LoadAsync(tempFile);

                // Assert
                Assert.NotNull(agent);
                Assert.Equal("test-approved-001", agent.Id.Value);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadAsync_EquivalentYamlAndJson_ProduceEquivalentAgentDefinitions()
        {
            // Arrange
            var yamlFile = Path.Combine(Path.GetTempPath(), $"test-agent-yaml-{Guid.NewGuid():N}.yaml");
            var jsonFile = Path.Combine(Path.GetTempPath(), $"test-agent-json-{Guid.NewGuid():N}.json");

            try
            {
                await File.WriteAllTextAsync(yamlFile, YamlApprovedAgent);
                await File.WriteAllTextAsync(jsonFile, JsonApprovedAgent);

                // Act
                var yamlAgent = await AgentDefinitionLoader.LoadAsync(yamlFile);
                var jsonAgent = await AgentDefinitionLoader.LoadAsync(jsonFile);

                // Assert - Core properties (Id will differ — each strict call generates new Guid)
                Assert.Equal(yamlAgent.Name, jsonAgent.Name);
                Assert.Equal(yamlAgent.Version, jsonAgent.Version);
                Assert.Equal(yamlAgent.Description, jsonAgent.Description);

                // Assert - Configuration
                Assert.Equal(yamlAgent.Configuration.Owner, jsonAgent.Configuration.Owner);
                Assert.Equal(yamlAgent.Configuration.RateLimitRequestsPerMinute, jsonAgent.Configuration.RateLimitRequestsPerMinute);
                Assert.Equal(yamlAgent.Configuration.TimeoutSeconds, jsonAgent.Configuration.TimeoutSeconds);
                Assert.Equal(yamlAgent.Configuration.RequiresAudit, jsonAgent.Configuration.RequiresAudit);

                // Assert - Collections
                Assert.Equal(yamlAgent.Configuration.AllowedActions.Count, jsonAgent.Configuration.AllowedActions.Count);
                // Expected: YAML has 2 actions, JSON should also have 2
                for (int i = 0; i < yamlAgent.Configuration.AllowedActions.Count; i++)
                {
                    Assert.Equal(yamlAgent.Configuration.AllowedActions[i], jsonAgent.Configuration.AllowedActions[i]);
                }

                Assert.Equal(yamlAgent.Configuration.Environments.Count, jsonAgent.Configuration.Environments.Count);
                Assert.Equal(yamlAgent.Rules.Count, jsonAgent.Rules.Count);
                Assert.Equal(yamlAgent.Tools.Count, jsonAgent.Tools.Count);
            }
            finally
            {
                if (File.Exists(yamlFile))
                    File.Delete(yamlFile);
                if (File.Exists(jsonFile))
                    File.Delete(jsonFile);
            }
        }

        [Fact]
        public async Task LoadAsync_MissingFile_ThrowsFileNotFoundException()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<FileNotFoundException>(
                () => AgentDefinitionLoader.LoadAsync("/nonexistent/path/agent.yaml"));

            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public async Task LoadAsync_UnsupportedExtension_UsesFlexibleMapper()
        {
            // The loader now accepts any extension via the flexible mapper fallback.
            // A .txt file containing valid YAML should return an AgentDefinition.
            var tempFile = Path.Combine(Path.GetTempPath(), $"test-agent-{Guid.NewGuid():N}.txt");
            try
            {
                await File.WriteAllTextAsync(tempFile, YamlApprovedAgent);

                // Act — should NOT throw (flexible mapper handles it)
                var agent = await AgentDefinitionLoader.LoadAsync(tempFile);

                Assert.NotNull(agent);
                // The flexible mapper should detect the name
                Assert.Equal("Test Approved Agent", agent.Name);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public async Task LoadAsync_NullOrEmptyPath_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AgentDefinitionLoader.LoadAsync(null!));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AgentDefinitionLoader.LoadAsync(""));

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => AgentDefinitionLoader.LoadAsync("   "));
        }

        [Fact]
        public void Load_SyncVersion_ReturnsValidAgentDefinition()
        {
            // Arrange
            var tempFile = Path.Combine(Path.GetTempPath(), $"test-agent-sync-{Guid.NewGuid():N}.json");
            try
            {
                File.WriteAllText(tempFile, JsonApprovedAgent);

                // Act
                var agent = AgentDefinitionLoader.Load(tempFile);

                // Assert
                Assert.NotNull(agent);
                Assert.Equal("test-approved-001", agent.Id.Value);
                Assert.Equal("Test Approved Agent", agent.Name);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
