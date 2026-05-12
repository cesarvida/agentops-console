using System;
using System.Collections;
using System.Collections.Generic;
using AgentOps.Core.Governance;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentOps.Infrastructure.Config
{
    /// <summary>
    /// Parses governance-config.yaml YAML strings into <see cref="GovernanceConfig"/>.
    /// Shared by <see cref="GovernanceConfigLoader"/> (GitHub API) and
    /// <see cref="LocalGovernanceConfigReader"/> (local file).
    /// </summary>
    public static class GovernanceConfigParser
    {
        /// <summary>
        /// Parses a governance-config.yaml YAML string.
        /// Returns <see cref="GovernanceConfig.Default"/> on any parse error.
        /// </summary>
        public static GovernanceConfig Parse(string yaml)
        {
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var raw = deserializer.Deserialize<Dictionary<string, object>>(yaml)
                          ?? new Dictionary<string, object>();

                // YAML wraps everything under 'governance:'
                if (!raw.TryGetValue("governance", out var govNode))
                    return GovernanceConfig.Default;

                var govDict = AsDict(govNode);
                if (govDict == null)
                    return GovernanceConfig.Default;

                var config = new GovernanceConfig
                {
                    Version = GetStr(govDict, "version", "1.0.0"),
                    Repo    = GetStr(govDict, "repo",    "")
                };

                // allowed_actions
                if (govDict.TryGetValue("allowed_actions", out var aa) && aa is IEnumerable aaList)
                {
                    config.AllowedActions = new List<string>();
                    foreach (var item in aaList)
                        if (item != null) config.AllowedActions.Add(item.ToString()!);
                }

                // forbidden_actions
                if (govDict.TryGetValue("forbidden_actions", out var fa) && fa is IEnumerable faList)
                {
                    config.ForbiddenActions = new List<string>();
                    foreach (var item in faList)
                        if (item != null) config.ForbiddenActions.Add(item.ToString()!);
                }

                // scoring
                if (govDict.TryGetValue("scoring", out var scoringNode))
                {
                    var sd = AsDict(scoringNode);
                    if (sd != null)
                    {
                        config.Scoring = new ScoringConfig
                        {
                            CriticalPenalty  = GetInt(sd, "critical_penalty",  25),
                            WarningPenalty   = GetInt(sd, "warning_penalty",   10),
                            BlockedThreshold = GetInt(sd, "blocked_threshold", 40),
                            ReviewThreshold  = GetInt(sd, "review_threshold",  70)
                        };
                    }
                }

                // audit
                if (govDict.TryGetValue("audit", out var auditNode))
                {
                    var ad = AsDict(auditNode);
                    if (ad != null)
                    {
                        config.Audit = new AuditConfig
                        {
                            Required         = GetBool(ad, "required",           true),
                            MinRetentionDays = GetInt(ad,  "min_retention_days", 30)
                        };
                    }
                }

                // environments
                if (govDict.TryGetValue("environments", out var envsNode))
                {
                    var envsDict = AsDict(envsNode);
                    if (envsDict != null)
                    {
                        foreach (var kv in envsDict)
                        {
                            var envCfg = AsDict(kv.Value);
                            if (envCfg == null) continue;

                            var ec = new EnvironmentConfig
                            {
                                RequireHumanApproval = GetBool(envCfg, "require_human_approval", false)
                            };

                            if (envCfg.TryGetValue("forbidden_actions_extra", out var extra) &&
                                extra is IEnumerable extraList)
                            {
                                foreach (var item in extraList)
                                    if (item != null) ec.ForbiddenActionsExtra.Add(item.ToString()!);
                            }

                            config.Environments[kv.Key] = ec;
                        }
                    }
                }

                // semantic_analysis
                if (govDict.TryGetValue("semantic_analysis", out var saNode))
                {
                    var sad = AsDict(saNode);
                    if (sad != null)
                    {
                        config.SemanticAnalysis = new SemanticAnalysisConfig
                        {
                            Enabled        = GetBool(sad, "enabled",         false),
                            Threshold      = GetStr(sad,  "threshold",       "MEDIUM"),
                            TimeoutSeconds = GetInt(sad,  "timeout_seconds", 5),
                            MaxTokens      = GetInt(sad,  "max_tokens",      800)
                        };
                    }
                }

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to parse governance-config.yaml: {ex.Message}. Using defaults.");
                return GovernanceConfig.Default;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static Dictionary<string, object>? AsDict(object? node)
        {
            if (node is Dictionary<object, object> untyped)
            {
                var r = new Dictionary<string, object>();
                foreach (var kv in untyped)
                    r[kv.Key?.ToString() ?? ""] = kv.Value;
                return r;
            }
            return node as Dictionary<string, object>;
        }

        private static string GetStr(Dictionary<string, object> d, string key, string fallback)
            => d.TryGetValue(key, out var v) && v != null ? v.ToString()! : fallback;

        private static int GetInt(Dictionary<string, object> d, string key, int fallback)
            => d.TryGetValue(key, out var v) && v != null && int.TryParse(v.ToString(), out int n) ? n : fallback;

        private static bool GetBool(Dictionary<string, object> d, string key, bool fallback)
            => d.TryGetValue(key, out var v) && v != null && bool.TryParse(v.ToString(), out bool b) ? b : fallback;
    }
}
