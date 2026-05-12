using System;
using System.Collections;
using System.Collections.Generic;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.ValueObjects;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentOps.Application.Governance
{
    /// <summary>
    /// Deserializes agent YAML definitions into <see cref="AgentDefinition"/> objects.
    /// Extracted from <see cref="ValidateAgentCommandHandler"/> so it can be reused by
    /// other components (e.g., GitHubAgentFetcher).
    /// </summary>
    public class AgentYamlDeserializer
    {
        private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        /// <summary>
        /// Deserializes a YAML string into an <see cref="AgentDefinition"/>.
        /// Uses defensive key access (TryGetValue) to tolerate missing optional fields.
        /// </summary>
        /// <param name="yaml">YAML string content.</param>
        /// <returns>A populated <see cref="AgentDefinition"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the YAML cannot be parsed.</exception>
        public AgentDefinition Deserialize(string yaml)
        {
            try
            {
                var raw = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yaml)
                          ?? new Dictionary<string, object>();

                string GetStr(string key, string fallback = "")
                {
                    if (raw.TryGetValue(key, out var val) && val != null)
                        return val.ToString()!;
                    return fallback;
                }

                Dictionary<string, object>? AsDict(object? node)
                {
                    if (node is Dictionary<object, object> untyped)
                    {
                        var result = new Dictionary<string, object>();
                        foreach (var kv in untyped)
                            result[kv.Key?.ToString() ?? ""] = kv.Value;
                        return result;
                    }
                    if (node is Dictionary<string, object> typed)
                        return typed;
                    return null;
                }

                string id      = GetStr("id",          Guid.NewGuid().ToString());
                string name    = GetStr("name",         "Unknown Agent");
                string version = GetStr("version",      "0.0.0").Trim('"');
                string desc    = GetStr("description",  "Agent loaded from YAML for governance validation");
                string owner   = GetStr("owner",        "");

                // Detect RequiresAudit: top-level 'audit' key OR inside 'governance.audit'
                bool requiresAudit = raw.ContainsKey("audit");
                if (!requiresAudit)
                {
                    raw.TryGetValue("governance", out var govNode);
                    var govDict = AsDict(govNode);
                    if (govDict != null)
                        requiresAudit = govDict.ContainsKey("audit");
                }

                var config = new AgentConfiguration
                {
                    Owner = owner,
                    RequiresAudit = requiresAudit
                };

                // Extract actions from top-level 'actions' list
                if (raw.TryGetValue("actions", out var actionsVal) && actionsVal is IEnumerable actions)
                {
                    foreach (var action in actions)
                        if (action != null) config.AllowedActions.Add(action.ToString()!);
                }

                // Fall back to governance.allowed_actions if present
                if (config.AllowedActions.Count == 0)
                {
                    raw.TryGetValue("governance", out var govNode2);
                    var govDict2 = AsDict(govNode2);
                    if (govDict2 != null &&
                        govDict2.TryGetValue("allowed_actions", out var govActions) &&
                        govActions is IEnumerable govActEnum)
                    {
                        foreach (var action in govActEnum)
                            if (action != null) config.AllowedActions.Add(action.ToString()!);
                    }
                }

                if (string.IsNullOrWhiteSpace(desc) || desc.Length < 10)
                    desc = $"Agent '{name}' loaded from YAML for governance validation";

                // ── Phase 9: new optional fields ────────────────────────────

                // rate_limit.requests_per_minute
                if (raw.TryGetValue("rate_limit", out var rateLimitNode))
                {
                    var rlDict = AsDict(rateLimitNode);
                    if (rlDict != null &&
                        rlDict.TryGetValue("requests_per_minute", out var rpm) &&
                        rpm != null &&
                        int.TryParse(rpm.ToString(), out int rpmVal))
                    {
                        config.RateLimitRequestsPerMinute = rpmVal;
                    }
                }

                // timeout_seconds
                if (raw.TryGetValue("timeout_seconds", out var timeoutVal) &&
                    timeoutVal != null &&
                    int.TryParse(timeoutVal.ToString(), out int timeoutSecs))
                {
                    config.TimeoutSeconds = timeoutSecs;
                }

                // environments list
                if (raw.TryGetValue("environments", out var envsVal) && envsVal is IEnumerable envsList)
                {
                    foreach (var env in envsList)
                        if (env != null) config.Environments.Add(env.ToString()!);
                }

                // exceptions list
                var exceptions = new List<GovernanceException>();
                if (raw.TryGetValue("exceptions", out var exceptionsVal) &&
                    exceptionsVal is IEnumerable exceptionsEnum)
                {
                    foreach (var item in exceptionsEnum)
                    {
                        var ex = AsDict(item);
                        if (ex == null) continue;

                        string ruleName   = ex.TryGetValue("rule",        out var r) && r != null ? r.ToString()! : "";
                        string reason     = ex.TryGetValue("reason",      out var rs) && rs != null ? rs.ToString()! : "";
                        string approvedBy = ex.TryGetValue("approved_by", out var ab) && ab != null ? ab.ToString()! : "";
                        string expiresStr = ex.TryGetValue("expires_at",  out var ea) && ea != null ? ea.ToString()! : "";

                        if (string.IsNullOrWhiteSpace(ruleName)) continue;

                        DateTime.TryParse(expiresStr, null,
                            System.Globalization.DateTimeStyles.RoundtripKind,
                            out DateTime expiresAt);

                        exceptions.Add(new GovernanceException
                        {
                            RuleName   = ruleName,
                            Reason     = reason,
                            ApprovedBy = approvedBy,
                            ExpiresAt  = expiresAt
                        });
                    }
                }

                return new AgentDefinition(
                    new AgentId(id),
                    name.Length >= 3 ? name : $"Agent-{id[..6]}",
                    desc,
                    purpose: "Governance validation from YAML",
                    rules:   new List<string> { "governance-validation" },
                    tools:   new List<string> { "governance-engine" },
                    configuration: config,
                    createdAt: DateTime.UtcNow,
                    version: string.IsNullOrWhiteSpace(version) ? "0.0.0" : version
                )
                {
                    Exceptions = exceptions
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deserializing agent YAML: {ex.Message}", ex);
            }
        }
    }
}
