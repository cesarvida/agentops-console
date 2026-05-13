using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AgentOps.Application.Parsing
{
    /// <summary>
    /// Parses any YAML or JSON content into a <see cref="Dictionary{string, object}"/>,
    /// with optional flattening of nested keys using dot-notation (e.g. "agent.name").
    /// Automatically detects format from content, not just file extension.
    /// </summary>
    public class UniversalDocumentParser
    {
        private static readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        /// <summary>
        /// Detects the format of the given content by inspecting the first non-whitespace character.
        /// Returns "JSON" if it starts with '{' or '[', otherwise "YAML".
        /// </summary>
        public string DetectFormat(string content)
        {
            var trimmed = (content ?? string.Empty).TrimStart();
            return (trimmed.StartsWith("{") || trimmed.StartsWith("[")) ? "JSON" : "YAML";
        }

        /// <summary>
        /// Parses the content (auto-detected as JSON or YAML) into a normalized
        /// <see cref="Dictionary{string, object}"/>.
        /// Returns an empty dictionary on any parse error — never throws.
        /// </summary>
        public Dictionary<string, object> Parse(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new Dictionary<string, object>();

            var trimmed = content.TrimStart();
            return (trimmed.StartsWith("{") || trimmed.StartsWith("["))
                ? ParseJson(content)
                : ParseYaml(content);
        }

        /// <summary>
        /// Flattens a nested dictionary into dot-notation keys.
        /// E.g. {"agent":{"name":"X"}} becomes {"agent.name":"X"} (plus the original entries).
        /// Both the original nested entries and the flat entries are included.
        /// </summary>
        public Dictionary<string, object> Flatten(Dictionary<string, object> nested, string prefix = "")
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in nested)
            {
                var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
                if (!result.ContainsKey(key))
                    result[key] = kvp.Value;

                if (kvp.Value is Dictionary<string, object> inner)
                {
                    foreach (var innerKvp in Flatten(inner, key))
                        if (!result.ContainsKey(innerKvp.Key))
                            result[innerKvp.Key] = innerKvp.Value;
                }
            }
            return result;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private Dictionary<string, object> ParseJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    return JsonObjectToDict(doc.RootElement);
            }
            catch { /* fall through to empty */ }
            return new Dictionary<string, object>();
        }

        private Dictionary<string, object> ParseYaml(string yaml)
        {
            try
            {
                var raw = _yamlDeserializer.Deserialize<object>(yaml);
                return NormalizeToDict(raw) ?? new Dictionary<string, object>();
            }
            catch { /* fall through to empty */ }
            return new Dictionary<string, object>();
        }

        private Dictionary<string, object> JsonObjectToDict(JsonElement element)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in element.EnumerateObject())
                result[prop.Name] = JsonElementToObject(prop.Value);
            return result;
        }

        private object JsonElementToObject(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String  => element.GetString() ?? "",
            JsonValueKind.True    => (object)true,
            JsonValueKind.False   => false,
            JsonValueKind.Null    => "",
            JsonValueKind.Number  => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
            JsonValueKind.Array   => JsonArrayToList(element),
            JsonValueKind.Object  => JsonObjectToDict(element),
            _                     => element.GetRawText()
        };

        private List<object> JsonArrayToList(JsonElement element)
        {
            var list = new List<object>();
            foreach (var item in element.EnumerateArray())
                list.Add(JsonElementToObject(item));
            return list;
        }

        private Dictionary<string, object>? NormalizeToDict(object? raw)
        {
            if (raw is Dictionary<object, object> untyped)
            {
                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in untyped)
                    result[kv.Key?.ToString() ?? ""] = NormalizeValue(kv.Value);
                return result;
            }
            if (raw is Dictionary<string, object> typed)
            {
                var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in typed)
                    result[kv.Key] = NormalizeValue(kv.Value);
                return result;
            }
            return null;
        }

        private object NormalizeValue(object? value)
        {
            if (value == null) return "";
            if (value is Dictionary<object, object> or Dictionary<string, object>)
                return NormalizeToDict(value) ?? new Dictionary<string, object>();
            if (value is string)
                return value;
            if (value is IList list)
            {
                var result = new List<object>();
                foreach (var item in list)
                    result.Add(NormalizeValue(item));
                return result;
            }
            return value;
        }
    }
}
