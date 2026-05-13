using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.ValueObjects;

namespace AgentOps.Application.Governance
{
    /// <summary>
    /// CQRS Command to validate an agent definition from a file (YAML or JSON).
    /// </summary>
    public class ValidateAgentCommand
    {
        /// <summary>Path to the agent definition file (supports .yaml, .yml, .json extensions).</summary>
        public string YamlPath { get; set; }

        public ValidateAgentCommand(string yamlPath)
        {
            YamlPath = yamlPath ?? throw new ArgumentNullException(nameof(yamlPath));
        }
    }

    /// <summary>
    /// Handler for ValidateAgentCommand that orchestrates governance evaluation.
    /// Optionally runs Azure OpenAI semantic analysis after the rule-based check
    /// when <see cref="IAgentSemanticAnalyzer"/> is provided and semantic analysis
    /// is enabled in the supplied <see cref="GovernanceConfig"/>.
    /// 
    /// Supports agent definitions in both YAML and JSON formats. The loader automatically
    /// detects the format based on file extension (.yaml, .yml, .json).
    /// </summary>
    public class ValidateAgentCommandHandler
    {
        private readonly GovernanceRuleEngine       _engine;
        private readonly IAgentSemanticAnalyzer?    _semanticAnalyzer;

        public ValidateAgentCommandHandler(
            GovernanceRuleEngine engine,
            IAgentSemanticAnalyzer? semanticAnalyzer = null)
        {
            _engine           = engine ?? throw new ArgumentNullException(nameof(engine));
            _semanticAnalyzer = semanticAnalyzer;
        }

        /// <summary>
        /// Executes the validation of an agent definition from a file or URL (YAML or JSON).
        /// Supports both local file paths and GitHub raw URLs.
        /// </summary>
        /// <param name="command">The validation command with file path or URL.</param>
        /// <param name="config">
        /// Optional governance config.  When null the engine uses its default config
        /// and semantic analysis is skipped.
        /// </param>
        /// <returns>A governance report with evaluation results.</returns>
        public async Task<GovernanceReport> HandleAsync(
            ValidateAgentCommand command,
            GovernanceConfig? config = null)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            // Detect if it's a URL or local file path
            bool isUrl = command.YamlPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                         command.YamlPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            AgentDefinition? agent = null;
            string yaml = string.Empty;

            try
            {
                if (isUrl)
                {
                    // Load from URL
                    agent = await AgentDefinitionLoader.LoadFromUrlAsync(command.YamlPath);
                    // For semantic analysis, we need the YAML content
                    using var client = new System.Net.Http.HttpClient();
                    yaml = await client.GetStringAsync(command.YamlPath);
                }
                else
                {
                    // Load from local file
                    if (!File.Exists(command.YamlPath))
                        throw new FileNotFoundException($"Agent definition file not found: {command.YamlPath}");

                    agent = await AgentDefinitionLoader.LoadAsync(command.YamlPath);
                    yaml = await File.ReadAllTextAsync(command.YamlPath);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load agent from {(isUrl ? "URL" : "file")}: {command.YamlPath}", ex);
            }

            if (agent == null)
                throw new InvalidOperationException("Failed to deserialize agent definition");

            // ── 1. Rule-based governance evaluation ──────────────────────────
            var report = config != null
                ? await _engine.EvaluateAsync(agent, config)
                : await _engine.EvaluateAsync(agent);

            // ── 2. Optional semantic analysis ────────────────────────────────
            var semanticConfig = config?.SemanticAnalysis ?? new SemanticAnalysisConfig();
            if (_semanticAnalyzer != null && semanticConfig.Enabled)
            {
                var semanticResult = await _semanticAnalyzer
                    .AnalyzeAgentSemanticsAsync(yaml, semanticConfig);

                report.SemanticAnalysis = semanticResult;

                // Merge semantic result into final status only when rules didn't already block
                if (report.FinalStatus != "BLOCKED" && semanticResult.IsAvailable)
                {
                    var risk = semanticResult.RiskLevel?.ToUpperInvariant() ?? "LOW";
                    if (risk == "HIGH")
                        report.FinalStatus = "BLOCKED";
                    else if (risk == "MEDIUM" && report.FinalStatus == "APPROVED")
                        report.FinalStatus = "REVIEW";
                }
            }

            // ── 3. Persist JSON report ────────────────────────────────────────
            await SaveReportAsJsonAsync(report);

            return report;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Saves the governance report as a JSON file in the outputs/ directory.
        /// </summary>
        private async Task SaveReportAsJsonAsync(GovernanceReport report)
        {
            try
            {
                const string outputDir = "outputs";
                Directory.CreateDirectory(outputDir);

                string safeId = string.IsNullOrWhiteSpace(report.AgentId)
                    ? "unknown"
                    : string.Concat(report.AgentId.Split(Path.GetInvalidFileNameChars()));
                string timestamp = report.EvaluatedAt.ToString("yyyyMMddHHmmss");
                string fileName  = $"governance-{safeId}-{timestamp}.json";
                string filePath  = Path.Combine(outputDir, fileName);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters    = { new JsonStringEnumConverter() }
                };
                string json = JsonSerializer.Serialize(report, options);
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Failed to save governance report JSON: {ex.Message}");
            }
        }
    }
}
