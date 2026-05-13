using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AgentOps.Application.Parsing;
using AgentOps.Core.Entities;

namespace AgentOps.Application.Governance
{
    /// <summary>
    /// Loads agent definitions from files in any YAML or JSON format.
    ///
    /// Loading strategy (strict-first, flexible-fallback):
    ///   1. Try the format-specific strict deserializer (YAML or JSON based on extension).
    ///   2. If the result is incomplete (no name and no actions found), fall back to
    ///      <see cref="FlexibleAgentMapper"/> which accepts any field structure.
    ///   3. Return an <see cref="AgentDefinition"/> always — never null, never throws on
    ///      unknown formats.
    ///
    /// For external format analysis (--external CLI flag) use
    /// <see cref="LoadWithContextAsync"/> to also receive <see cref="AgentMappingContext"/>.
    /// </summary>
    public static class AgentDefinitionLoader
    {
        private static readonly AgentYamlDeserializer    _yamlDeserializer = new();
        private static readonly AgentJsonDeserializer    _jsonDeserializer = new();
        private static readonly UniversalDocumentParser  _universalParser  = new();
        private static readonly FlexibleAgentMapper      _flexibleMapper   = new();

        // ── Primary API (backward-compatible) ────────────────────────────────

        /// <summary>
        /// Loads an agent definition from a file path.
        /// Accepts .yaml, .yml, .json, and any other extension (via the flexible mapper).
        /// </summary>
        public static async Task<AgentDefinition> LoadAsync(string filePath)
        {
            var (agent, _) = await LoadWithContextAsync(filePath);
            return agent;
        }

        /// <summary>Synchronous overload of <see cref="LoadAsync"/>.</summary>
        public static AgentDefinition Load(string filePath)
        {
            var (agent, _) = LoadWithContext(filePath);
            return agent;
        }

        // ── Extended API (includes mapping metadata) ──────────────────────────

        /// <summary>
        /// Loads an agent definition and returns an <see cref="AgentMappingContext"/> that
        /// describes how the source document was translated.  Used by the CLI --external flag.
        /// </summary>
        public static async Task<(AgentDefinition Agent, AgentMappingContext Context)>
            LoadWithContextAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Agent definition file not found: {filePath}");

            var content   = await File.ReadAllTextAsync(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return ResolveAgent(content, extension, filePath);
        }

        /// <summary>Synchronous overload of <see cref="LoadWithContextAsync"/>.</summary>
        public static (AgentDefinition Agent, AgentMappingContext Context)
            LoadWithContext(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Agent definition file not found: {filePath}");

            var content   = File.ReadAllText(filePath);
            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return ResolveAgent(content, extension, filePath);
        }

        // ── URL-based loading (new) ───────────────────────────────────────────

        /// <summary>
        /// Loads an agent definition from an HTTP URL (GitHub raw, etc).
        /// Detects format from URL extension or content inspection.
        /// </summary>
        public static async Task<AgentDefinition> LoadFromUrlAsync(string url)
        {
            var (agent, _) = await LoadFromUrlWithContextAsync(url);
            return agent;
        }

        /// <summary>
        /// Loads an agent definition from an HTTP URL and returns mapping context.
        /// </summary>
        public static async Task<(AgentDefinition Agent, AgentMappingContext Context)>
            LoadFromUrlWithContextAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url), "URL cannot be null or empty");

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Invalid URL: {url}");

            using var client = new HttpClient();
            try
            {
                var content = await client.GetStringAsync(url);
                var extension = Path.GetExtension(url).ToLowerInvariant();
                
                // If no extension or unusual, default to .json for detection
                if (string.IsNullOrEmpty(extension) || extension.Length > 5)
                    extension = ".json";

                return ResolveAgent(content, extension, url);
            }
            catch (HttpRequestException ex)
            {
                throw new IOException($"Failed to download agent from URL: {url}", ex);
            }
        }

        // ── Internal resolution logic ─────────────────────────────────────────

        private static (AgentDefinition Agent, AgentMappingContext Context)
            ResolveAgent(string content, string extension, string filePath)
        {
            var detectedFormat = _universalParser.DetectFormat(content);

            // ── Step 1: Try strict deserializer ───────────────────────────────
            AgentDefinition? strictResult = null;
            try
            {
                strictResult = extension switch
                {
                    ".yaml" or ".yml" => _yamlDeserializer.Deserialize(content),
                    ".json"           => _jsonDeserializer.Deserialize(content),
                    _                 => null   // Unknown extension → skip to flexible
                };
            }
            catch { /* strict failed → fall through to flexible */ }

            if (strictResult != null && IsComplete(strictResult))
            {
                var strictCtx = new AgentMappingContext
                {
                    DetectedFormat     = detectedFormat,
                    UsedFlexibleMapper = false,
                    SourceFile         = filePath
                };
                return (strictResult, strictCtx);
            }

            // ── Step 2: Flexible mapper fallback ─────────────────────────────
            var rawDoc = _universalParser.Parse(content);
            return _flexibleMapper.Map(rawDoc, filePath, detectedFormat);
        }

        /// <summary>
        /// An agent is "complete" when it has a real name (not the default placeholder)
        /// OR has at least one declared action. Incomplete agents are re-processed
        /// by the flexible mapper to extract more fields.
        /// </summary>
        private static bool IsComplete(AgentDefinition agent) =>
            agent.Name != "Unknown Agent"
            || (agent.Configuration?.AllowedActions?.Any() == true);
    }
}

