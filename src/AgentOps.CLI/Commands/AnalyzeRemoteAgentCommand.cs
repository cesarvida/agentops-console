using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.Governance;
using AgentOps.Application.Interfaces;
using AgentOps.Application.Rules;
using AgentOps.CLI.Rules;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using Microsoft.Extensions.DependencyInjection;

namespace AgentOps.CLI.Commands
{
    /// <summary>
    /// Interactive single-pass remote agent analyzer.
    /// Prompts user for agent URL and rules, then executes full analysis in one run.
    /// 
    /// Usage:
    /// dotnet run -- analyze
    /// 
    /// Then answers the prompts.
    /// </summary>
    public class AnalyzeRemoteAgentCommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly GovernanceRuleEngine _ruleEngine;
        private readonly UserRulesLoader _rulesLoader;

        public AnalyzeRemoteAgentCommand(
            IServiceProvider serviceProvider,
            GovernanceRuleEngine ruleEngine,
            UserRulesLoader rulesLoader)
        {
            _serviceProvider = serviceProvider;
            _ruleEngine = ruleEngine;
            _rulesLoader = rulesLoader;
        }

        /// <summary>
        /// Interactive prompt-based analyzer. Asks for URL, then rules, then executes.
        /// </summary>
        public async Task<int> ExecuteInteractiveAsync()
        {
            try
            {
                PrintHeader("🔍 AGENT ANALYZER");
                Console.WriteLine();

                // ── PROMPT 1: Get agent URL ──────────────────────────────────
                var agentUrl = PromptForURL();
                if (string.IsNullOrEmpty(agentUrl))
                    return 1;

                Console.WriteLine();

                // ── PROMPT 2: Get governance rules ───────────────────────────
                var (allowedActions, forbiddenActions, requireOwner, requireAudit, minScore, blockScore) 
                    = PromptForRules();

                Console.WriteLine();

                // ── STEP 1: Load agent from URL ──────────────────────────────
                Console.WriteLine("📥 Loading agent from GitHub...");
                AgentDefinition agent;
                try
                {
                    agent = await AgentDefinitionLoader.LoadFromUrlAsync(agentUrl);
                }
                catch (Exception ex)
                {
                    PrintError($"Failed to load agent: {ex.Message}");
                    return 1;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Loaded: {agent.Name} v{agent.Version}");
                Console.ResetColor();
                Console.WriteLine();

                // ── STEP 2: Build governance config ──────────────────────────
                Console.WriteLine("⚙️  Building governance rules...");
                var userRules = _rulesLoader.LoadFromFlags(
                    allowedActions,
                    forbiddenActions,
                    requireOwner,
                    requireAudit,
                    minScore,
                    blockScore);
                var governanceConfig = userRules.ToGovernanceConfig();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Rules configured");
                Console.ResetColor();
                Console.WriteLine();

                // ── STEP 3: Run governance evaluation ────────────────────────
                Console.WriteLine("🔬 Running governance analysis...");
                var report = await _ruleEngine.EvaluateAsync(agent, governanceConfig);

                // ── STEP 4: Run semantic analysis ────────────────────────────
                if (governanceConfig.SemanticAnalysis?.Enabled == true)
                {
                    Console.WriteLine("🧠 Running semantic analysis...");
                    var semanticAnalyzer = _serviceProvider.GetService<IAgentSemanticAnalyzer>();
                    if (semanticAnalyzer != null)
                    {
                        var yaml = await LoadRawAgentAsync(agentUrl);
                        var semanticResult = await semanticAnalyzer.AnalyzeAgentSemanticsAsync(
                            yaml,
                            governanceConfig.SemanticAnalysis);

                        report.SemanticAnalysis = semanticResult;

                        // Merge semantic into final status
                        if (report.FinalStatus != "BLOCKED" && semanticResult.IsAvailable)
                        {
                            var risk = semanticResult.RiskLevel?.ToUpperInvariant() ?? "LOW";
                            if (risk == "HIGH")
                                report.FinalStatus = "BLOCKED";
                            else if (risk == "MEDIUM" && report.FinalStatus == "APPROVED")
                                report.FinalStatus = "REVIEW";
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("⚠️  Semantic analysis not available (Azure OpenAI not configured)");
                        Console.ResetColor();
                    }
                }

                Console.WriteLine();

                // ── STEP 5: Display results ──────────────────────────────────
                DisplayAnalysisResult(agent, report);
                Console.WriteLine();

                return report.FinalStatus == "APPROVED" ? 0 : 1;
            }
            catch (Exception ex)
            {
                PrintError($"Error: {ex.Message}");
                return 1;
            }
        }

        private string PromptForURL()
        {
            Console.WriteLine("🌐 Enter Agent URL");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Examples:");
            Console.WriteLine("  https://raw.githubusercontent.com/owner/repo/branch/path/agent.yaml");
            Console.WriteLine("  https://raw.githubusercontent.com/owner/repo/main/agent.json");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("Agent URL: ");

            var url = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(url))
            {
                PrintError("URL cannot be empty");
                return string.Empty;
            }

            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                PrintError("URL must start with http:// or https://");
                return string.Empty;
            }

            return url;
        }

        private (string?, string?, bool?, bool?, int?, int?) PromptForRules()
        {
            Console.WriteLine("📋 Configure Governance Rules");
            Console.WriteLine();

            // Allowed actions
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("ALLOWED ACTIONS (comma-separated):");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Examples: read_code, post_comment, create_report, read_files");
            Console.ResetColor();
            Console.Write("Allowed actions: ");
            var allowedStr = Console.ReadLine()?.Trim();
            var allowed = string.IsNullOrEmpty(allowedStr) ? null : allowedStr;

            Console.WriteLine();

            // Forbidden actions
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("FORBIDDEN ACTIONS (comma-separated):");
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Examples: delete_repo, dangerous_api, execute_code");
            Console.ResetColor();
            Console.Write("Forbidden actions: ");
            var forbiddenStr = Console.ReadLine()?.Trim();
            var forbidden = string.IsNullOrEmpty(forbiddenStr) ? null : forbiddenStr;

            Console.WriteLine();

            // Owner required
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("OWNER REQUIRED? (y/n):");
            Console.ResetColor();
            Console.Write("Require owner: ");
            var ownerStr = Console.ReadLine()?.Trim().ToLower();
            bool? ownerRequired = ownerStr == "y" ? true : ownerStr == "n" ? false : (bool?)null;

            Console.WriteLine();

            // Audit required
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("AUDIT LOGGING REQUIRED? (y/n):");
            Console.ResetColor();
            Console.Write("Require audit: ");
            var auditStr = Console.ReadLine()?.Trim().ToLower();
            bool? auditRequired = auditStr == "y" ? true : auditStr == "n" ? false : (bool?)null;

            return (allowed, forbidden, ownerRequired, auditRequired, null, null);
        }

        private void PrintHeader(string title)
        {
            var width = 60;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('═', width));
            Console.WriteLine($"║ {title.PadRight(width - 4)} ║");
            Console.WriteLine(new string('═', width));
            Console.ResetColor();
            Console.WriteLine();
        }

        private void DisplayAnalysisResult(AgentDefinition agent, GovernanceReport report)
        {
            var width = 60;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('═', width));
            Console.WriteLine("ANALYSIS RESULT");
            Console.WriteLine(new string('═', width));
            Console.ResetColor();
            Console.WriteLine();

            // Agent info
            Console.Write("Agent:   ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{agent.Name} v{agent.Version}");
            Console.ResetColor();

            if (agent.Configuration?.Owner != null)
            {
                Console.Write("Owner:   ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(agent.Configuration.Owner);
                Console.ResetColor();
            }

            Console.WriteLine();

            // Rule results
            if (report.RuleResults?.Any() == true)
            {
                Console.WriteLine("Rules:");
                foreach (var result in report.RuleResults)
                {
                    var icon = result.Severity == RuleSeverity.Critical ? "❌" :
                              result.Severity == RuleSeverity.Warning ? "⚠️ " : "✅";
                    var color = result.Severity == RuleSeverity.Critical ? ConsoleColor.Red :
                               result.Severity == RuleSeverity.Warning ? ConsoleColor.Yellow : ConsoleColor.Green;

                    Console.ForegroundColor = color;
                    Console.WriteLine($"  {icon} {result.RuleName}");
                    Console.ResetColor();

                    if (result.Violations?.Any() == true)
                    {
                        foreach (var violation in result.Violations.Take(2))
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine($"     • {violation}");
                            Console.ResetColor();
                        }
                    }
                }

                Console.WriteLine();
            }

            // Semantic analysis
            if (report.SemanticAnalysis?.IsAvailable == true)
            {
                Console.Write("Semantic: ");
                var riskColor = report.SemanticAnalysis.RiskLevel switch
                {
                    "HIGH" => ConsoleColor.Red,
                    "MEDIUM" => ConsoleColor.Yellow,
                    _ => ConsoleColor.Green
                };
                Console.ForegroundColor = riskColor;
                Console.WriteLine(report.SemanticAnalysis.RiskLevel);
                Console.ResetColor();
                Console.WriteLine();
            }

            // Score and status
            Console.Write("Score:   ");
            Console.ForegroundColor = GetScoreColor(report.GovernanceScore);
            Console.WriteLine($"{report.GovernanceScore}/100");
            Console.ResetColor();

            Console.Write("Status:  ");
            var statusColor = report.FinalStatus == "APPROVED" ? ConsoleColor.Green :
                             report.FinalStatus == "REVIEW" ? ConsoleColor.Yellow : ConsoleColor.Red;
            Console.ForegroundColor = statusColor;
            Console.WriteLine(report.FinalStatus);
            Console.ResetColor();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('═', width));
            Console.ResetColor();
        }

        private ConsoleColor GetScoreColor(int score)
        {
            return score >= 70 ? ConsoleColor.Green :
                   score >= 50 ? ConsoleColor.Yellow : ConsoleColor.Red;
        }

        private void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ {message}");
            Console.ResetColor();
        }

        private async Task<string> LoadRawAgentAsync(string url)
        {
            using var client = new System.Net.Http.HttpClient();
            return await client.GetStringAsync(url);
        }
    }
}

