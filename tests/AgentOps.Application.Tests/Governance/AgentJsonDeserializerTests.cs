using System;
using System.Collections.Generic;
using AgentOps.Application.Governance;
using AgentOps.Core.Entities;
using Xunit;

namespace AgentOps.Application.Tests.Governance
{
    /// <summary>
    /// Tests for <see cref="AgentJsonDeserializer"/> to verify correct JSON parsing
    /// and mapping to AgentDefinition model properties.
    /// </summary>
    public class AgentJsonDeserializerTests
    {
        private readonly AgentJsonDeserializer _deserializer = new();

        [Fact]
        public void Deserialize_MinimalJsonAgent_ReturnsValidDefinition()
        {
            // Arrange
            var json = @"{
                ""id"": ""minimal-agent"",
                ""name"": ""Minimal Agent"",
                ""description"": ""A minimal test agent with required fields"",
                ""purpose"": ""Testing minimal configuration"",
                ""rules"": [""rule1""],
                ""tools"": [""tool1""]
            }";

            // Act
            var agent = _deserializer.Deserialize(json);

            // Assert
            Assert.NotNull(agent);
            Assert.Equal("minimal-agent", agent.Id.Value);
            Assert.Equal("Minimal Agent", agent.Name);
            Assert.Equal("A minimal test agent with required fields", agent.Description);
            // Rules and tools use hard-coded values matching YAML deserializer
            Assert.Contains("governance-validation", agent.Rules);
            Assert.Contains("governance-engine", agent.Tools);
        }

        [Fact]
        public void Deserialize_CompleteJsonAgent_ParsesAllFields()
        {
            // Arrange
            var json = @"{
                ""id"": ""complete-agent"",
                ""name"": ""Complete Agent"",
                ""version"": ""2.0.0"",
                ""description"": ""A complete agent with all optional fields"",
                ""purpose"": ""Testing complete configuration"",
                ""owner"": ""team-complete"",
                ""actions"": [""read"", ""write"", ""delete""],
                ""rules"": [""rule1"", ""rule2""],
                ""tools"": [""tool1"", ""tool2""],
                ""rate_limit"": {
                    ""requests_per_minute"": 150
                },
                ""timeout_seconds"": 45,
                ""environments"": [""prod"", ""staging""],
                ""audit"": {
                    ""log_all_actions"": true,
                    ""retention_days"": 60
                }
            }";

            // Act
            var agent = _deserializer.Deserialize(json);

            // Assert
            Assert.Equal("complete-agent", agent.Id.Value);
            Assert.Equal("Complete Agent", agent.Name);
            Assert.Equal("2.0.0", agent.Version);
            Assert.Equal("team-complete", agent.Configuration.Owner);
            Assert.Equal(150, agent.Configuration.RateLimitRequestsPerMinute);
            Assert.Equal(45, agent.Configuration.TimeoutSeconds);
            Assert.Contains("read", agent.Configuration.AllowedActions);
            Assert.Contains("prod", agent.Configuration.Environments);
            Assert.True(agent.Configuration.RequiresAudit);
            // Rules and tools use hard-coded values
            Assert.Contains("governance-validation", agent.Rules);
            Assert.Contains("governance-engine", agent.Tools);
        }

        [Fact]
        public void Deserialize_JsonWithExceptions_ParsesGovernanceExceptions()
        {
            // Arrange
            var json = @"{
                ""id"": ""exception-agent"",
                ""name"": ""Agent with Exceptions"",
                ""description"": ""Testing governance exceptions parsing"",
                ""purpose"": ""Exception test"",
                ""rules"": [""rule1""],
                ""tools"": [""tool1""],
                ""exceptions"": [
                    {
                        ""rule"": ""critical_rule"",
                        ""reason"": ""Temporary exception for testing"",
                        ""approved_by"": ""admin@example.com"",
                        ""expires_at"": ""2025-12-31T23:59:59Z""
                    }
                ]
            }";

            // Act
            var agent = _deserializer.Deserialize(json);

            // Assert
            Assert.NotEmpty(agent.Exceptions);
            Assert.Equal("critical_rule", agent.Exceptions[0].RuleName);
            Assert.Equal("Temporary exception for testing", agent.Exceptions[0].Reason);
            Assert.Equal("admin@example.com", agent.Exceptions[0].ApprovedBy);
        }

        [Fact]
        public void Deserialize_JsonWithSnakeCaseProperties_MapsCorrectly()
        {
            // Arrange - JSON uses snake_case, C# should use PascalCase
            var json = @"{
                ""id"": ""snake-case-agent"",
                ""name"": ""Snake Case Test"",
                ""description"": ""Testing snake_case property mapping"",
                ""purpose"": ""Property name conversion test"",
                ""owner"": ""team-snake"",
                ""rate_limit"": {
                    ""requests_per_minute"": 200
                },
                ""timeout_seconds"": 120,
                ""rules"": [""rule1""],
                ""tools"": [""tool1""]
            }";

            // Act
            var agent = _deserializer.Deserialize(json);

            // Assert
            Assert.Equal("team-snake", agent.Configuration.Owner);
            Assert.Equal(200, agent.Configuration.RateLimitRequestsPerMinute);
            Assert.Equal(120, agent.Configuration.TimeoutSeconds);
        }

        [Fact]
        public void Deserialize_JsonWithMissingOptionalFields_UsesDefaults()
        {
            // Arrange
            var json = @"{
                ""id"": ""minimal-agent"",
                ""name"": ""Minimal Test"",
                ""description"": ""Minimal agent without optional fields"",
                ""purpose"": ""Default values test"",
                ""rules"": [""rule1""],
                ""tools"": [""tool1""]
            }";

            // Act
            var agent = _deserializer.Deserialize(json);

            // Assert
            Assert.Equal("", agent.Configuration.Owner);  // Default empty string
            Assert.Null(agent.Configuration.RateLimitRequestsPerMinute);
            Assert.Null(agent.Configuration.TimeoutSeconds);
            Assert.Empty(agent.Configuration.Environments);
            Assert.False(agent.Configuration.RequiresAudit);
        }

        [Fact]
        public void Deserialize_InvalidJson_ThrowsInvalidOperationException()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => _deserializer.Deserialize(invalidJson));
            Assert.Contains("Failed to deserialize", ex.Message);
        }

        [Fact]
        public void Deserialize_JsonWithoutRequiredName_UsesDefault()
        {
            // Arrange
            var json = @"{
                ""id"": ""nameless-agent"",
                ""description"": ""Agent without name"",
                ""purpose"": ""Test missing name"",
                ""rules"": [""rule1""],
                ""tools"": [""tool1""]
            }";

            // Act
            var agent = _deserializer.Deserialize(json);

            // Assert
            Assert.Equal("Unknown Agent", agent.Name);
        }

        [Fact]
        public void Deserialize_JsonWithEmptyArrays_HandlesCorrectly()
        {
            // Arrange
            var json = @"{
                ""id"": ""empty-arrays"",
                ""name"": ""Empty Collections"",
                ""description"": ""Testing empty array handling"",
                ""purpose"": ""Empty array test"",
                ""actions"": [],
                ""environments"": [],
                ""rules"": [""rule1""],
                ""tools"": [""tool1""]
            }";

            // Act
            var agent = _deserializer.Deserialize(json);

            // Assert
            Assert.Empty(agent.Configuration.AllowedActions);
            Assert.Empty(agent.Configuration.Environments);
            Assert.Single(agent.Rules);
            Assert.Single(agent.Tools);
        }
    }
}
