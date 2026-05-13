using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using AgentOps.Application.Rules;
using AgentOps.Core.Rules;

namespace AgentOps.Application.Tests.Rules
{
    public class UserRulesLoaderTests
    {
    private string GetTestRulesDir()
    {
        // From bin/Release/net10.0/ we need to go up 5 levels to reach app_console/
        // net10.0/ -> Release/ -> bin/ -> AgentOps.Application.Tests/ -> tests/ -> app_console/
        var baseDir = AppContext.BaseDirectory; // bin/Release/net10.0
        var appConsoleRoot = Path.Combine(baseDir, "..", "..", "..", "..", "..");
        var normalized = Path.GetFullPath(appConsoleRoot);
        return Path.Combine(normalized, "data", "rules");
    }

        /// <summary>
        /// Test 1: Load default-rules.yaml → reglas correctas
        /// </summary>
        [Fact]
        public async Task LoadFromFileAsync_DefaultRules_LoadsCorrectly()
        {
            var loader = new UserRulesLoader();
            var rulesPath = Path.Combine(GetTestRulesDir(), "default-rules.yaml");

            var rules = await loader.LoadFromFileAsync(rulesPath);

            Assert.NotNull(rules);
            Assert.Equal("Default Rules", rules.Name);
            Assert.Contains("read_code", rules.AllowedActions);
            Assert.Contains("post_comment", rules.AllowedActions);
            Assert.Contains("push_to_main", rules.ForbiddenActions);
            Assert.True(rules.OwnerRequired);
            Assert.True(rules.AuditRequired);
            Assert.Equal(40, rules.BlockedThreshold);
            Assert.Equal(70, rules.ReviewThreshold);
        }

        /// <summary>
        /// Test 2: Load strict-rules.yaml → thresholds más estrictos
        /// </summary>
        [Fact]
        public async Task LoadFromFileAsync_StrictRules_ThresholdsAreStricter()
        {
            var loader = new UserRulesLoader();
            var rulesPath = Path.Combine(GetTestRulesDir(), "strict-rules.yaml");

            var rules = await loader.LoadFromFileAsync(rulesPath);

            Assert.NotNull(rules);
            Assert.Equal("Strict Production Rules", rules.Name);
            Assert.Equal(60, rules.BlockedThreshold);  // stricter than default 40
            Assert.Equal(85, rules.ReviewThreshold);   // stricter than default 70
            Assert.True(rules.OwnerRequired);
            Assert.True(rules.AuditRequired);
        }

        /// <summary>
        /// Test 3: Flags CLI sobreescriben archivo → merge correcto
        /// </summary>
        [Fact]
        public void Merge_FlagsOverrideFile_ThresholdUpdated()
        {
            var loader = new UserRulesLoader();
            
            var rulesFromFile = new UserRules
            {
                Name = "File Rules",
                BlockedThreshold = 40,
                ReviewThreshold = 70
            };

            var rulesFromFlags = new UserRules
            {
                BlockedThreshold = 50,
                ReviewThreshold = 80
            };

            var merged = loader.Merge(rulesFromFile, rulesFromFlags);

            Assert.Equal(50, merged.BlockedThreshold);
            Assert.Equal(80, merged.ReviewThreshold);
            Assert.Equal("File Rules", merged.Name);
        }

        /// <summary>
        /// Test 4: --allow añade acción al whitelist del archivo
        /// </summary>
        [Fact]
        public void LoadFromFlags_AllowedActions_ParsesCorrectly()
        {
            var loader = new UserRulesLoader();
            
            var rules = loader.LoadFromFlags(
                allowedActions: "read_code,post_comment,send_notification",
                forbiddenActions: null,
                requireOwner: null,
                requireAudit: null,
                minScore: null,
                blockScore: null
            );

            Assert.NotNull(rules);
            Assert.Contains("read_code", rules.AllowedActions);
            Assert.Contains("post_comment", rules.AllowedActions);
            Assert.Contains("send_notification", rules.AllowedActions);
        }

        /// <summary>
        /// Test 5: --forbid añade acción al blacklist del archivo
        /// </summary>
        [Fact]
        public void LoadFromFlags_ForbiddenActions_ParsesCorrectly()
        {
            var loader = new UserRulesLoader();
            
            var rules = loader.LoadFromFlags(
                allowedActions: null,
                forbiddenActions: "push_to_main,delete_files,execute_code",
                requireOwner: null,
                requireAudit: null,
                minScore: null,
                blockScore: null
            );

            Assert.NotNull(rules);
            Assert.Contains("push_to_main", rules.ForbiddenActions);
            Assert.Contains("delete_files", rules.ForbiddenActions);
            Assert.Contains("execute_code", rules.ForbiddenActions);
        }

        /// <summary>
        /// Test 6: UserRules.ToGovernanceConfig() → GovernanceConfig válido
        /// </summary>
        [Fact]
        public void ToGovernanceConfig_ConvertsCorrectly()
        {
            var rules = new UserRules
            {
                AllowedActions = new List<string> { "read_code", "post_comment" },
                ForbiddenActions = new List<string> { "push_to_main", "delete_files" },
                CriticalPenalty = 30,
                WarningPenalty = 15,
                BlockedThreshold = 50,
                ReviewThreshold = 80,
                AuditRequired = true
            };

            var config = rules.ToGovernanceConfig();

            Assert.NotNull(config);
            Assert.Contains("read_code", config.AllowedActions);
            Assert.Contains("push_to_main", config.ForbiddenActions);
            Assert.Equal(30, config.Scoring.CriticalPenalty);
            Assert.Equal(15, config.Scoring.WarningPenalty);
            Assert.Equal(50, config.Scoring.BlockedThreshold);
            Assert.Equal(80, config.Scoring.ReviewThreshold);
            Assert.True(config.Audit.Required);
        }

        /// <summary>
        /// Test 7: GetDefaults() → retorna reglas por defecto válidas
        /// </summary>
        [Fact]
        public void GetDefaults_ReturnsValidDefaults()
        {
            var loader = new UserRulesLoader();
            
            var defaults = loader.GetDefaults();

            Assert.NotNull(defaults);
            Assert.Equal("Default Rules", defaults.Name);
            Assert.NotEmpty(defaults.AllowedActions);
            Assert.NotEmpty(defaults.ForbiddenActions);
            Assert.True(defaults.OwnerRequired);
            Assert.True(defaults.AuditRequired);
            Assert.Equal(25, defaults.CriticalPenalty);
            Assert.Equal(10, defaults.WarningPenalty);
        }

        /// <summary>
        /// Test 8: Sin ningún flag → usa defaults, no lanza excepción
        /// </summary>
        [Fact]
        public void LoadFromFlags_NoFlags_ReturnsEmpty()
        {
            var loader = new UserRulesLoader();
            
            var rules = loader.LoadFromFlags(
                allowedActions: null,
                forbiddenActions: null,
                requireOwner: null,
                requireAudit: null,
                minScore: null,
                blockScore: null
            );

            Assert.NotNull(rules);
            Assert.Empty(rules.AllowedActions);
            Assert.Empty(rules.ForbiddenActions);
        }
    }
}
