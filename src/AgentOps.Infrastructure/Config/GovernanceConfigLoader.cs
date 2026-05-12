using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Governance;
using AgentOps.GitHub;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentOps.Infrastructure.Config
{
    /// <summary>
    /// Loads the governance configuration for a GitHub repository by fetching
    /// <c>data/governance-config.yaml</c> via the GitHub Contents API.
    /// Returns <see cref="GovernanceConfig.Default"/> when the file is absent or
    /// the token is not configured.
    /// </summary>
    public class GovernanceConfigLoader : IGovernanceConfigLoader
    {
        private const string ConfigPath = "data/governance-config.yaml";

        private readonly GitHubHttpClient _httpClient;

        private static readonly IDeserializer _yaml = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        public GovernanceConfigLoader(GitHubHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <inheritdoc />
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

                return ParseConfig(yaml);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[INFO] Could not load governance-config.yaml from {owner}/{repo}: {ex.Message}. Using defaults.");
                return GovernanceConfig.Default;
            }
        }

        // ── YAML parsing ────────────────────────────────────────────────────

        private static GovernanceConfig ParseConfig(string yaml)
        {
            try
            {
                // Deserialize to raw dict so we can handle the nested 'governance:' wrapper
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var raw = deserializer.Deserialize<Dictionary<string, object>>(yaml)
                          ?? new Dictionary<string, object>();

                // The YAML wraps everything under 'governance:'
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

                return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to parse governance-config.yaml: {ex.Message}. Using defaults.");
                return GovernanceConfig.Default;
            }
        }

        // ── Dict helpers ────────────────────────────────────────────────────

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

        // ── GitHub API DTO ──────────────────────────────────────────────────

        private sealed class GitHubFile
        {
            public string Content  { get; set; } = string.Empty;
            public string Encoding { get; set; } = string.Empty;
        }
    }
}
