using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.ValueObjects;

namespace AgentOps.Application.Governance
{
    /// <summary>
    /// Deserializes agent JSON definitions into <see cref="AgentDefinition"/> objects.
    /// Handles snake_case JSON property names and maps them to PascalCase C# properties.
    /// </summary>
    public class AgentJsonDeserializer
    {
        /// <summary>
        /// Deserializes a JSON string into an <see cref="AgentDefinition"/>.
        /// Uses defensive key access to tolerate missing optional fields.
        /// </summary>
        /// <param name="json">JSON string content.</param>
        /// <returns>A populated <see cref="AgentDefinition"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the JSON cannot be parsed.</exception>
        public AgentDefinition Deserialize(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Helper functions for safe property access
                string GetStr(string key, string fallback = "")
                {
                    if (root.TryGetProperty(key, out var element))
                    {
                        if (element.ValueKind == JsonValueKind.String)
                            return element.GetString() ?? fallback;
                    }
                    return fallback;
                }

                // Core fields
                string id      = GetStr("id", Guid.NewGuid().ToString());
                string name    = GetStr("name", "Unknown Agent");
                string version = GetStr("version", "0.0.0");
                string desc    = GetStr("description", "Agent loaded from JSON for governance validation");
                string owner   = GetStr("owner", "");
                string purpose = GetStr("purpose", $"Agent '{name}' loaded from JSON for governance validation");

                // Parse rules and tools lists
                // NOTE: Currently, both YAML and JSON deserializers use hard-coded defaults
                // to match existing YAML deserialization behavior. If actual rules/tools from
                // the definition file are needed in the future, parse from JSON here.
                var rules = new List<string> { "governance-validation" };
                var tools = new List<string> { "governance-engine" };

                // Build configuration
                var config = new AgentConfiguration
                {
                    Owner = owner,
                    RequiresAudit = root.TryGetProperty("audit", out _)
                };

                // Extract actions from top-level 'actions' list
                var actions = ExtractStringList(root, "actions");
                foreach (var action in actions)
                {
                    if (!string.IsNullOrWhiteSpace(action))
                        config.AllowedActions.Add(action);
                }

                // Fall back to configuration.allowed_actions if present
                if (config.AllowedActions.Count == 0 && root.TryGetProperty("configuration", out var configElement))
                {
                    if (configElement.ValueKind == JsonValueKind.Object)
                    {
                        var allowedActions = ExtractStringList(configElement, "allowed_actions");
                        foreach (var action in allowedActions)
                        {
                            if (!string.IsNullOrWhiteSpace(action))
                                config.AllowedActions.Add(action);
                        }
                    }
                }

                // Ensure description is not too short
                if (string.IsNullOrWhiteSpace(desc) || desc.Length < 10)
                    desc = $"Agent '{name}' loaded from JSON for governance validation";

                // ── Phase 9: optional fields ─────────────────────────────────

                // rate_limit.requests_per_minute
                if (root.TryGetProperty("rate_limit", out var rateLimitElement) &&
                    rateLimitElement.ValueKind == JsonValueKind.Object)
                {
                    if (rateLimitElement.TryGetProperty("requests_per_minute", out var rpm) &&
                        rpm.ValueKind == JsonValueKind.Number &&
                        rpm.TryGetInt32(out int rpmVal))
                    {
                        config.RateLimitRequestsPerMinute = rpmVal;
                    }
                }

                // timeout_seconds
                if (root.TryGetProperty("timeout_seconds", out var timeoutElement) &&
                    timeoutElement.ValueKind == JsonValueKind.Number &&
                    timeoutElement.TryGetInt32(out int timeoutSecs))
                {
                    config.TimeoutSeconds = timeoutSecs;
                }

                // environments list
                var environments = ExtractStringList(root, "environments");
                foreach (var env in environments)
                {
                    if (!string.IsNullOrWhiteSpace(env))
                        config.Environments.Add(env);
                }

                // exceptions list
                var exceptions = new List<GovernanceException>();
                if (root.TryGetProperty("exceptions", out var exceptionsElement) &&
                    exceptionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in exceptionsElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object) continue;

                        string ruleName   = ExtractPropertyString(item, "rule");
                        string reason     = ExtractPropertyString(item, "reason");
                        string approvedBy = ExtractPropertyString(item, "approved_by");
                        string expiresStr = ExtractPropertyString(item, "expires_at");

                        if (string.IsNullOrWhiteSpace(ruleName)) continue;

                        var exception = new GovernanceException
                        {
                            RuleName   = ruleName,
                            Reason     = reason,
                            ApprovedBy = approvedBy,
                            ExpiresAt  = !string.IsNullOrWhiteSpace(expiresStr) && DateTime.TryParse(expiresStr, out var expiresAt)
                                ? expiresAt
                                : DateTime.UtcNow.AddDays(30)  // Default to 30 days if not specified
                        };

                        exceptions.Add(exception);
                    }
                }

                // Build the agent definition
                var agent = new AgentDefinition(
                    new AgentId(id),
                    name,
                    desc,
                    purpose,
                    rules,
                    tools,
                    config,
                    DateTime.UtcNow,
                    version
                )
                {
                    Exceptions = exceptions
                };

                return agent;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to deserialize JSON agent definition: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deserializing agent definition from JSON: {ex.Message}", ex);
            }
        }

        private static List<string> ExtractStringList(JsonElement element, string propertyName)
        {
            var result = new List<string>();
            if (element.TryGetProperty(propertyName, out var arrayElement) && arrayElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arrayElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && item.GetString() is string str)
                    {
                        if (!string.IsNullOrWhiteSpace(str))
                            result.Add(str);
                    }
                }
            }
            return result;
        }

        private static string ExtractPropertyString(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString() ?? "";
            return "";
        }
    }

    // Custom JSON converters for better type handling (kept for future use if needed)
    internal class IntListConverter : JsonConverter<List<int>>
    {
        public override List<int> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<int>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
                if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int value))
                    list.Add(value);
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<int> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
                writer.WriteNumberValue(item);
            writer.WriteEndArray();
        }
    }

    internal class StringListConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var list = new List<string>();
            if (reader.TokenType != JsonTokenType.StartArray)
                return list;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;
                if (reader.TokenType == JsonTokenType.String && reader.GetString() is string value)
                    list.Add(value);
            }
            return list;
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
                writer.WriteStringValue(item);
            writer.WriteEndArray();
        }
    }
}

