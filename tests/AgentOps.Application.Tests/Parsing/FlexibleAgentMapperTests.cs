using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.Governance;
using AgentOps.Application.Parsing;
using AgentOps.Core.Governance;
using Xunit;

namespace AgentOps.Application.Tests.Parsing
{
    /// <summary>
    /// Tests for <see cref="FlexibleAgentMapper"/> and <see cref="UniversalDocumentParser"/>
    /// validating that external agent formats from OpenAI, LangChain, or custom structures
    /// are correctly mapped to <see cref="AgentOps.Core.Entities.AgentDefinition"/>.
    /// </summary>
    public class FlexibleAgentMapperTests
    {
        private readonly FlexibleAgentMapper      _mapper = new();
        private readonly UniversalDocumentParser  _parser = new();

        // ── Test 1: OpenAI format uses "capabilities" as actions ──────────────

        [Fact]
        public void Map_OpenAIFormat_CapabilitiesMappedToActions()
        {
            var raw = _parser.Parse(@"{
                ""assistant_name"": ""CodeHelper"",
                ""version"": ""2.1.0"",
                ""author"": ""openai-team"",
                ""capabilities"": [""read_code"", ""post_comment"", ""push_to_main""]
            }");

            var (agent, ctx) = _mapper.Map(raw, detectedFormat: "JSON");

            Assert.Equal("CodeHelper", agent.Name);
            Assert.Contains("read_code",    agent.Configuration.AllowedActions);
            Assert.Contains("post_comment", agent.Configuration.AllowedActions);
            Assert.Contains("push_to_main", agent.Configuration.AllowedActions);
            Assert.Contains("capabilities → actions", string.Join("|", ctx.MappingNotes));
        }

        // ── Test 2: "author" maps to owner ────────────────────────────────────

        [Fact]
        public void Map_AuthorField_MappedToOwner()
        {
            var raw = _parser.Parse(@"{
                ""name"": ""TestBot"",
                ""author"": ""backend-team"",
                ""skills"": [""read_code""]
            }");

            var (agent, ctx) = _mapper.Map(raw, detectedFormat: "JSON");

            Assert.Equal("backend-team", agent.Configuration.Owner);
            Assert.True(ctx.MappingNotes.Any(n => n.Contains("author") && n.Contains("owner")));
        }

        // ── Test 3: No version field → defaults to "dev" (triggers rule) ──────

        [Fact]
        public void Map_NoVersionField_DefaultsToDev()
        {
            var raw = _parser.Parse(@"{
                ""name"": ""VersionlessBot"",
                ""skills"": [""read_code""]
            }");

            var (agent, ctx) = _mapper.Map(raw, detectedFormat: "JSON");

            Assert.Equal("dev", agent.Version);
            Assert.True(ctx.MappingNotes.Any(n => n.Contains("VersionDefinedRule")));
        }

        // ── Test 4: Forbidden actions in any field are detected ───────────────

        [Fact]
        public void Map_ForbiddenActionsAnywhereInDoc_DetectedViaDeepScan()
        {
            // "bypass_authentication" is in "other_capabilities", not in "skills"
            var raw = _parser.Parse(@"{
                ""name"": ""DangerBot"",
                ""skills"": [""read_code""],
                ""other_capabilities"": [""bypass_authentication""]
            }");

            var (agent, _) = _mapper.Map(raw, detectedFormat: "JSON");

            // Deep scan should add bypass_authentication to AllowedActions
            Assert.Contains("bypass_authentication", agent.Configuration.AllowedActions);
        }

        // ── Test 5: Flatten produces dot-notation keys ────────────────────────

        [Fact]
        public void Flatten_NestedDocument_ProducesDotNotationKeys()
        {
            var nested = new Dictionary<string, object>
            {
                ["agent"] = new Dictionary<string, object>
                {
                    ["name"]   = "MyBot",
                    ["config"] = new Dictionary<string, object>
                    {
                        ["timeout"] = 30L
                    }
                }
            };

            var flat = _parser.Flatten(nested);

            Assert.True(flat.ContainsKey("agent.name"));
            Assert.True(flat.ContainsKey("agent.config.timeout"));
            Assert.Equal("MyBot", flat["agent.name"].ToString());
        }

        // ── Test 6: Empty/unknown JSON → AgentDefinition with safe defaults ───

        [Fact]
        public void Map_PurelyUnknownJson_ReturnsDefaultsWithoutThrowing()
        {
            var raw = _parser.Parse(@"{
                ""llm_backend"": ""openai"",
                ""memory_config"": { ""type"": ""buffer"" },
                ""temperature"": 0.7
            }");

            var (agent, ctx) = _mapper.Map(raw, detectedFormat: "JSON");

            Assert.NotNull(agent);
            Assert.Equal("Unknown Agent", agent.Name);
            Assert.True(ctx.UsedFlexibleMapper);
            // All fields are unrecognized
            Assert.True(ctx.UnrecognizedFields.Count > 0);
        }

        // ── Test 7: Completely different YAML structure → no exception thrown ─

        [Fact]
        public void Map_ArbitraryYamlStructure_NeverThrows()
        {
            var raw = _parser.Parse(@"
pipeline:
  name: DataPipeline
  steps:
    - extract
    - transform
    - load
config:
  retry: 3
  timeout: 120
");

            var exception = Record.Exception(() => _mapper.Map(raw, detectedFormat: "YAML"));
            Assert.Null(exception);
        }

        // ── Test 8: openai-assistant.json → BLOCKED due to push_to_main ──────

        [Fact]
        public async Task LoadAsync_OpenAIAssistantJson_IsBlockedByGovernance()
        {
            var filePath = Path.Combine(
                FindSamplesDir(), "external-agents", "openai-assistant.json");

            if (!File.Exists(filePath))
            {
                // Skip if samples not present (e.g., running from sub-directory)
                return;
            }

            var agent = await AgentDefinitionLoader.LoadAsync(filePath);
            var engine = BuildDefaultEngine();
            var report = await engine.EvaluateAsync(agent, GovernanceConfig.Default);

            Assert.Equal("BLOCKED", report.FinalStatus);
            Assert.Contains(agent.Configuration.AllowedActions, a =>
                string.Equals(a, "push_to_main", StringComparison.OrdinalIgnoreCase));
        }

        // ── Test 9: custom-bot.yaml → APPROVED ────────────────────────────────

        [Fact]
        public async Task LoadAsync_CustomBotYaml_IsApprovedByGovernance()
        {
            var filePath = Path.Combine(
                FindSamplesDir(), "external-agents", "custom-bot.yaml");

            if (!File.Exists(filePath))
                return;

            var agent  = await AgentDefinitionLoader.LoadAsync(filePath);
            var engine = BuildDefaultEngine();
            var report = await engine.EvaluateAsync(agent, GovernanceConfig.Default);

            // custom-bot has only safe read/create actions
            Assert.NotEqual("BLOCKED", report.FinalStatus);
        }

        // ── Test 10: dangerous-agent.json → BLOCKED, score < 40 ──────────────

        [Fact]
        public async Task LoadAsync_DangerousAgentJson_BlockedWithLowScore()
        {
            var filePath = Path.Combine(
                FindSamplesDir(), "external-agents", "dangerous-agent.json");

            if (!File.Exists(filePath))
                return;

            var agent  = await AgentDefinitionLoader.LoadAsync(filePath);
            var engine = BuildDefaultEngine();
            var report = await engine.EvaluateAsync(agent, GovernanceConfig.Default);

            Assert.Equal("BLOCKED", report.FinalStatus);
            Assert.True(report.GovernanceScore < 40,
                $"Expected score < 40 but got {report.GovernanceScore}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static AgentOps.Application.Governance.GovernanceRuleEngine BuildDefaultEngine()
        {
            return new AgentOps.Application.Governance.GovernanceRuleEngine(
                new AgentOps.Core.Governance.IGovernanceRule[]
                {
                    new AgentOps.Core.Governance.Rules.AllowedActionsRule(),
                    new AgentOps.Core.Governance.Rules.ForbiddenActionsRule(),
                    new AgentOps.Core.Governance.Rules.AuditLoggingRule(),
                    new AgentOps.Core.Governance.Rules.OwnerDefinedRule(),
                    new AgentOps.Core.Governance.Rules.VersionDefinedRule(),
                    new AgentOps.Core.Governance.Rules.RateLimitRule(),
                    new AgentOps.Core.Governance.Rules.TimeoutRule(),
                    new AgentOps.Core.Governance.Rules.EnvironmentScopeRule()
                });
        }

        /// <summary>
        /// Walks up the directory tree from the executing assembly to find the
        /// repo root (contains AgentOps.Console.sln), then returns the samples/ folder.
        /// </summary>
        private static string FindSamplesDir()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir, "AgentOps.Console.sln")))
                    return Path.Combine(dir, "samples");
                dir = Path.GetDirectoryName(dir);
            }
            return Path.Combine(AppContext.BaseDirectory, "samples");
        }
    }
}
