using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.ValueObjects;

namespace AgentOps.Application.Parsing
{
    /// <summary>
    /// Maps any <see cref="Dictionary{string,object}"/> (the result of parsing any YAML or JSON)
    /// to an <see cref="AgentDefinition"/> using a priority-ordered list of field aliases.
    ///
    /// Field lookup strategy:
    ///   1. Checks exact key match in combined dict (original + flattened)
    ///   2. First alias that yields a non-empty value wins
    ///
    /// Additionally scans ALL string values in the document for forbidden actions
    /// so that dangerous capabilities declared anywhere (not just in "actions") are caught.
    /// </summary>
    public class FlexibleAgentMapper
    {
        private readonly UniversalDocumentParser _parser = new();

        // ── Ordered alias lists per semantic field ────────────────────────────

        private static readonly string[] NameAliases =
        {
            "name", "agent_name", "bot_name", "assistant_name",
            "title", "agent.name", "agent.id", "bot.id", "id"
        };

        private static readonly string[] VersionAliases =
        {
            "version", "agent_version", "v", "ver",
            "metadata.version", "info.version", "bot.v"
        };

        private static readonly string[] OwnerAliases =
        {
            "owner", "author", "maintainer", "team", "created_by",
            "contact", "metadata.owner", "bot.team"
        };

        private static readonly string[] ActionsAliases =
        {
            "actions", "capabilities", "permissions", "tools",
            "allowed_operations", "functions", "skills",
            "agent.actions", "agent.capabilities", "agent.tools",
            "bot.permissions"
        };

        private static readonly string[] AuditAliases =
        {
            "audit", "logging", "audit_config", "log_config",
            "observability", "monitoring", "monitor", "bot.monitor"
        };

        private static readonly string[] EnvironmentsAliases =
        {
            "environments", "envs", "environment", "deploy_to",
            "targets", "deployment.environments", "bot.targets"
        };

        private static readonly string[] DescriptionAliases =
        {
            "description", "desc", "about", "summary"
        };

        private static readonly HashSet<string> AllKnownKeys = new(
            NameAliases
                .Concat(VersionAliases)
                .Concat(OwnerAliases)
                .Concat(ActionsAliases)
                .Concat(AuditAliases)
                .Concat(EnvironmentsAliases)
                .Concat(DescriptionAliases),
            StringComparer.OrdinalIgnoreCase);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Maps the raw parsed document to an <see cref="AgentDefinition"/> using flexible
        /// field alias resolution. Also returns an <see cref="AgentMappingContext"/> describing
        /// what was detected and how it was mapped.
        /// </summary>
        /// <param name="raw">Normalized dictionary from <see cref="UniversalDocumentParser.Parse"/>.</param>
        /// <param name="sourceFile">Optional source file path (for display only).</param>
        /// <param name="detectedFormat">Format string: "JSON" or "YAML".</param>
        /// <param name="forbiddenActions">
        /// Optional override for forbidden action list. Defaults to
        /// <see cref="GovernanceConfig.DefaultForbiddenActions"/>.
        /// </param>
        public (AgentDefinition Agent, AgentMappingContext Context) Map(
            Dictionary<string, object> raw,
            string sourceFile = "",
            string detectedFormat = "Unknown",
            IReadOnlyList<string>? forbiddenActions = null)
        {
            var context = new AgentMappingContext
            {
                SourceFile     = sourceFile,
                DetectedFormat = detectedFormat,
                UsedFlexibleMapper = true
            };

            forbiddenActions ??= GovernanceConfig.DefaultForbiddenActions;

            // Build a combined lookup: original keys + flattened dot-notation keys
            var flat = _parser.Flatten(raw);
            var combined = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in raw)    combined[kvp.Key] = kvp.Value;
            foreach (var kvp in flat)   if (!combined.ContainsKey(kvp.Key)) combined[kvp.Key] = kvp.Value;

            context.TotalFieldCount = flat.Count;

            // Categorize top-level keys as recognized or unrecognized
            foreach (var key in raw.Keys)
            {
                if (AllKnownKeys.Contains(key))
                    context.RecognizedFields.Add(key);
                else
                    context.UnrecognizedFields.Add(key);
            }
            // Also check flattened keys for recognition
            foreach (var key in flat.Keys)
            {
                if (AllKnownKeys.Contains(key) && !context.RecognizedFields.Contains(key))
                    context.RecognizedFields.Add(key);
            }

            // ── Field resolution ──────────────────────────────────────────────

            // Name
            string name = FindString(combined, NameAliases, out string? nameKey) ?? "Unknown Agent";
            if (nameKey != null && nameKey != "name")
                context.MappingNotes.Add($"{nameKey} → name");

            // Version
            string? foundVersion = FindString(combined, VersionAliases, out string? versionKey);
            string version = foundVersion ?? "dev";    // "dev" triggers VersionDefinedRule Warning
            if (versionKey != null && versionKey != "version")
                context.MappingNotes.Add($"{versionKey} → version");
            if (versionKey == null)
                context.MappingNotes.Add("Sin versión detectada → aplicando regla VersionDefinedRule");

            // Owner
            string? owner = FindString(combined, OwnerAliases, out string? ownerKey);
            if (ownerKey != null && ownerKey != "owner")
                context.MappingNotes.Add($"{ownerKey} → owner");
            if (ownerKey == null)
                context.MappingNotes.Add("Sin owner detectado → aplicando regla OwnerDefinedRule");

            // Actions (from alias mapping)
            (List<string> mappedActions, string? actionsKey) = FindList(combined, ActionsAliases);
            if (actionsKey != null && actionsKey != "actions")
                context.MappingNotes.Add(
                    $"{actionsKey} → actions ({mappedActions.Count} elementos detectados)");

            // Audit presence
            bool requiresAudit = false;
            foreach (var alias in AuditAliases)
            {
                if (combined.TryGetValue(alias, out var auditVal) && auditVal != null
                    && auditVal.ToString() != "")
                {
                    requiresAudit = true;
                    if (alias != "audit")
                        context.MappingNotes.Add($"{alias} → audit");
                    break;
                }
            }
            if (!requiresAudit)
                context.MappingNotes.Add("Sin configuración de audit → aplicando regla AuditLoggingRule");

            // Environments
            (List<string> environments, string? envsKey) = FindList(combined, EnvironmentsAliases);
            if (envsKey != null && envsKey != "environments")
                context.MappingNotes.Add(
                    $"{envsKey} → environments ({environments.Count} elementos)");
            if (envsKey == null)
                context.MappingNotes.Add("Sin environments detectados → aplicando regla EnvironmentScopeRule");

            // Description
            string desc = FindString(combined, DescriptionAliases, out _) ?? "";
            if (string.IsNullOrWhiteSpace(desc) || desc.Length < 10)
                desc = $"Agent '{name}' analizado desde {detectedFormat} por AgentOps universal parser";

            // ── Deep scan: forbidden actions in ANY document value ────────────
            var allDocValues = CollectAllStringValues(raw)
                .Select(v => v.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var deepForbidden = forbiddenActions
                .Where(fa => allDocValues.Contains(fa))
                .Where(fa => !mappedActions.Contains(fa, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (deepForbidden.Count > 0)
                context.MappingNotes.Add(
                    $"Acciones prohibidas detectadas en el documento: {string.Join(", ", deepForbidden)}");

            // Merge alias-mapped actions + deep-scanned forbidden actions
            var allActions = new List<string>(mappedActions);
            foreach (var fa in deepForbidden)
                if (!allActions.Contains(fa, StringComparer.OrdinalIgnoreCase))
                    allActions.Add(fa);

            // ── Build AgentConfiguration ──────────────────────────────────────
            var config = new AgentConfiguration
            {
                Owner         = owner ?? "",
                RequiresAudit = requiresAudit
            };
            foreach (var a in allActions)  config.AllowedActions.Add(a);
            foreach (var e in environments) config.Environments.Add(e);

            // ── Ensure AgentDefinition constraints ────────────────────────────
            if (name.Length < 3) name = name.PadRight(3, '_');
            if (desc.Length < 10) desc = $"External agent from {detectedFormat}: {name}";

            try
            {
                var agent = new AgentDefinition(
                    new AgentId(Guid.NewGuid().ToString()),
                    name,
                    desc,
                    "External agent analyzed by AgentOps universal parser",
                    new List<string> { "governance-validation" },
                    new List<string> { "governance-engine" },
                    config,
                    DateTime.UtcNow,
                    version
                );
                return (agent, context);
            }
            catch (Exception)
            {
                // Final safety net: normalize any remaining constraint violations
                var safeAgent = new AgentDefinition(
                    new AgentId(Guid.NewGuid().ToString()),
                    name.Length >= 3 ? name : "External Agent",
                    desc.Length >= 10 ? desc : $"External agent parsed from {detectedFormat}",
                    "External agent analyzed by AgentOps universal parser",
                    new List<string> { "governance-validation" },
                    new List<string> { "governance-engine" },
                    config,
                    DateTime.UtcNow,
                    string.IsNullOrWhiteSpace(version) ? "dev" : version
                );
                return (safeAgent, context);
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private static string? FindString(
            Dictionary<string, object> dict,
            string[] aliases,
            out string? foundKey)
        {
            foreach (var alias in aliases)
            {
                if (dict.TryGetValue(alias, out var val) && val != null)
                {
                    var str = val.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        foundKey = alias;
                        return str;
                    }
                }
            }
            foundKey = null;
            return null;
        }

        private static (List<string> Items, string? FoundKey) FindList(
            Dictionary<string, object> dict,
            string[] aliases)
        {
            foreach (var alias in aliases)
            {
                if (dict.TryGetValue(alias, out var val) && val != null)
                {
                    var list = ExtractStringList(val);
                    if (list.Count > 0)
                        return (list, alias);
                }
            }
            return (new List<string>(), null);
        }

        private static List<string> ExtractStringList(object val)
        {
            var result = new List<string>();
            if (val is string s)
            {
                // Treat comma-separated single strings
                result.AddRange(s.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            else if (val is IEnumerable<object> typedList)
            {
                foreach (var item in typedList)
                    if (item?.ToString() is string str && !string.IsNullOrWhiteSpace(str))
                        result.Add(str.Trim());
            }
            else if (val is IEnumerable otherList)
            {
                foreach (var item in otherList)
                    if (item?.ToString() is string str && !string.IsNullOrWhiteSpace(str))
                        result.Add(str.Trim());
            }
            return result;
        }

        /// <summary>Recursively collects every string leaf value in the document.</summary>
        private static IEnumerable<string> CollectAllStringValues(object? node)
        {
            switch (node)
            {
                case string s:
                    yield return s;
                    break;
                case Dictionary<string, object> dict:
                    foreach (var v in dict.Values)
                        foreach (var sv in CollectAllStringValues(v))
                            yield return sv;
                    break;
                case IEnumerable<object> list:
                    foreach (var item in list)
                        foreach (var sv in CollectAllStringValues(item))
                            yield return sv;
                    break;
                case IEnumerable otherList when node is not string:
                    foreach (var item in otherList)
                        if (item != null)
                            foreach (var sv in CollectAllStringValues(item))
                                yield return sv;
                    break;
            }
        }
    }
}
