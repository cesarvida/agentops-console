using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using AgentOps.Core.Rules;

namespace AgentOps.Application.Rules
{
    /// <summary>
    /// Loads user-defined rules from YAML files or CLI flags.
    /// Supports merging flags with file-based rules.
    /// </summary>
    public class UserRulesLoader
    {
        private readonly IDeserializer _yamlDeserializer;

        public UserRulesLoader()
        {
            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        /// <summary>
        /// Loads rules from a YAML file.
        /// </summary>
        public async Task<UserRules> LoadFromFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Rules file not found: {filePath}");
            }

            var content = await File.ReadAllTextAsync(filePath);
            var yamlContent = _yamlDeserializer.Deserialize<Dictionary<string, object>>(content);

            if (yamlContent == null || !yamlContent.ContainsKey("rules"))
            {
                throw new InvalidOperationException("Invalid rules file format. Expected 'rules' root key.");
            }

            var rulesDict = yamlContent["rules"] as Dictionary<object, object>;
            if (rulesDict == null)
            {
                throw new InvalidOperationException("Invalid rules section.");
            }

            var rules = ParseRulesDict(rulesDict);
            return rules;
        }

        /// <summary>
        /// Loads rules from CLI flags.
        /// </summary>
        public UserRules LoadFromFlags(
            string? allowedActions = null,
            string? forbiddenActions = null,
            bool? requireOwner = null,
            bool? requireAudit = null,
            int? minScore = null,
            int? blockScore = null)
        {
            var rules = new UserRules();

            if (!string.IsNullOrEmpty(allowedActions))
            {
                rules.AllowedActions = allowedActions
                    .Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(forbiddenActions))
            {
                rules.ForbiddenActions = forbiddenActions
                    .Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();
            }

            if (requireOwner.HasValue)
                rules.OwnerRequired = requireOwner.Value;

            if (requireAudit.HasValue)
                rules.AuditRequired = requireAudit.Value;

            if (minScore.HasValue)
                rules.ReviewThreshold = minScore.Value;

            if (blockScore.HasValue)
                rules.BlockedThreshold = blockScore.Value;

            return rules;
        }

        /// <summary>
        /// Merges file-based rules with CLI flags.
        /// CLI flags override file values.
        /// For lists (allowed/forbidden actions), CLI flags are ADDED to file values.
        /// </summary>
        public UserRules Merge(UserRules fromFile, UserRules fromFlags)
        {
            var merged = new UserRules
            {
                Name = fromFile.Name,
                Description = fromFile.Description,
                OwnerRequired = fromFlags.OwnerRequired != fromFile.OwnerRequired ? fromFlags.OwnerRequired : fromFile.OwnerRequired,
                AuditRequired = fromFlags.AuditRequired != fromFile.AuditRequired ? fromFlags.AuditRequired : fromFile.AuditRequired,
                VersionRequired = fromFile.VersionRequired,
                CriticalPenalty = fromFile.CriticalPenalty,
                WarningPenalty = fromFile.WarningPenalty,
                BlockedThreshold = fromFlags.BlockedThreshold != 40 ? fromFlags.BlockedThreshold : fromFile.BlockedThreshold,
                ReviewThreshold = fromFlags.ReviewThreshold != 70 ? fromFlags.ReviewThreshold : fromFile.ReviewThreshold
            };

            // For allowed actions: merge file + flags
            merged.AllowedActions = new List<string>(fromFile.AllowedActions);
            if (fromFlags.AllowedActions.Count > 0)
            {
                foreach (var action in fromFlags.AllowedActions)
                {
                    if (!merged.AllowedActions.Contains(action))
                    {
                        merged.AllowedActions.Add(action);
                    }
                }
            }

            // For forbidden actions: merge file + flags
            merged.ForbiddenActions = new List<string>(fromFile.ForbiddenActions);
            if (fromFlags.ForbiddenActions.Count > 0)
            {
                foreach (var action in fromFlags.ForbiddenActions)
                {
                    if (!merged.ForbiddenActions.Contains(action))
                    {
                        merged.ForbiddenActions.Add(action);
                    }
                }
            }

            return merged;
        }

        /// <summary>
        /// Returns default rules.
        /// </summary>
        public UserRules GetDefaults()
        {
            return UserRules.GetDefaults();
        }

        // ── Helper methods ──────────────────────────────────────────────

        private UserRules ParseRulesDict(Dictionary<object, object> rulesDict)
        {
            var rules = new UserRules();

            if (rulesDict.TryGetValue("name", out var nameObj))
                rules.Name = nameObj?.ToString() ?? rules.Name;

            if (rulesDict.TryGetValue("description", out var descObj))
                rules.Description = descObj?.ToString() ?? rules.Description;

            // Parse actions
            if (rulesDict.TryGetValue("actions", out var actionsObj) && actionsObj is Dictionary<object, object> actionsDict)
            {
                rules.AllowedActions = ParseActionsList(actionsDict, "allowed");
                rules.ForbiddenActions = ParseActionsList(actionsDict, "forbidden");
            }

            // Parse requirements
            if (rulesDict.TryGetValue("requirements", out var reqObj) && reqObj is Dictionary<object, object> reqDict)
            {
                if (reqDict.TryGetValue("owner_required", out var ownerObj))
                    rules.OwnerRequired = ParseBool(ownerObj);

                if (reqDict.TryGetValue("audit_required", out var auditObj))
                    rules.AuditRequired = ParseBool(auditObj);

                if (reqDict.TryGetValue("version_required", out var versionObj))
                    rules.VersionRequired = ParseBool(versionObj);
            }

            // Parse scoring
            if (rulesDict.TryGetValue("scoring", out var scoringObj) && scoringObj is Dictionary<object, object> scoringDict)
            {
                if (scoringDict.TryGetValue("critical_penalty", out var cpObj))
                    rules.CriticalPenalty = ParseInt(cpObj);

                if (scoringDict.TryGetValue("warning_penalty", out var wpObj))
                    rules.WarningPenalty = ParseInt(wpObj);

                if (scoringDict.TryGetValue("blocked_threshold", out var btObj))
                    rules.BlockedThreshold = ParseInt(btObj);

                if (scoringDict.TryGetValue("review_threshold", out var rtObj))
                    rules.ReviewThreshold = ParseInt(rtObj);
            }

            return rules;
        }

        private List<string> ParseActionsList(Dictionary<object, object> actionsDict, string key)
        {
            var result = new List<string>();

            if (actionsDict.TryGetValue(key, out var actionsObj))
            {
                if (actionsObj is List<object> actionsList)
                {
                    result = actionsList
                        .Select(a => a?.ToString() ?? "")
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToList();
                }
            }

            return result;
        }

        private bool ParseBool(object? obj)
        {
            if (obj == null) return false;
            if (bool.TryParse(obj.ToString(), out var result))
                return result;
            return obj.ToString()?.ToLower() == "true";
        }

        private int ParseInt(object? obj)
        {
            if (obj == null) return 0;
            if (int.TryParse(obj.ToString(), out var result))
                return result;
            return 0;
        }
    }
}
