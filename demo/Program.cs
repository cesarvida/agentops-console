using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Governance;
using AgentOps.Core.Governance.Rules;
using AgentOps.Application.Governance;
using AgentOps.Infrastructure.AzureOpenAI;

namespace AgentOps.Demo
{
    /// <summary>
    /// Governance Demo — runs validation against three sample agents and shows results.
    ///
    /// DEMO_SEMANTIC_MODE environment variable:
    ///   "fake"  — uses FakeAgentSemanticAnalyzer (deterministic, no Azure calls)
    ///   "real"  — uses the real Azure OpenAI client if env vars are present, otherwise skips
    ///   (unset) — defaults to "fake"
    /// </summary>
    internal static class Program
    {
        private static readonly string AgentsDir =
            Path.Combine(AppContext.BaseDirectory, "agents");

        private static readonly (string File, string SemanticRisk)[] DemoAgents = new[]
        {
            ("approved-agent.yaml", "LOW"),
            ("review-agent.yaml",   "MEDIUM"),
            ("blocked-agent.yaml",  "HIGH"),
        };

        static async Task<int> Main(string[] args)
        {
            PrintBanner();

            var semanticMode = (Environment.GetEnvironmentVariable("DEMO_SEMANTIC_MODE") ?? "fake")
                               .Trim().ToLowerInvariant();

            Console.WriteLine($"Semantic mode : {semanticMode.ToUpper()}");
            Console.WriteLine();

            var engine  = BuildEngine();
            var overall = 0;

            foreach (var (agentFile, semanticRisk) in DemoAgents)
            {
                var yamlPath = Path.Combine(AgentsDir, agentFile);
                if (!File.Exists(yamlPath))
                {
                    Console.WriteLine($"⚠️  File not found: {yamlPath}");
                    continue;
                }

                IAgentSemanticAnalyzer? analyzer = semanticMode == "fake"
                    ? new FakeAgentSemanticAnalyzer(semanticRisk)
                    : TryBuildRealAnalyzer();

                var config = new GovernanceConfig
                {
                    SemanticAnalysis = new SemanticAnalysisConfig
                    {
                        Enabled        = true,
                        Threshold      = "MEDIUM",
                        TimeoutSeconds = 5,
                        MaxTokens      = 800
                    }
                };

                var handler = new ValidateAgentCommandHandler(engine, analyzer);
                var report  = await handler.HandleAsync(new ValidateAgentCommand(yamlPath), config);

                PrintReport(agentFile, report);

                if (report.FinalStatus == "BLOCKED")
                    overall = 1;
            }

            Console.WriteLine(new string('━', 60));
            Console.WriteLine();

            return overall;
        }

        // ── Engine ────────────────────────────────────────────────────────────

        private static GovernanceRuleEngine BuildEngine() =>
            new GovernanceRuleEngine(new IGovernanceRule[]
            {
                new AllowedActionsRule(),
                new ForbiddenActionsRule(),
                new AuditLoggingRule(),
                new OwnerDefinedRule(),
                new VersionDefinedRule(),
                new RateLimitRule(),
                new TimeoutRule(),
                new EnvironmentScopeRule(),
            });

        // ── Real Azure analyzer (optional) ────────────────────────────────────

        private static IAgentSemanticAnalyzer? TryBuildRealAnalyzer()
        {
            var endpoint   = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var apiKey     = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-5.4-nano";

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("ℹ️  Azure credentials not set — semantic analysis will be skipped.");
                return null;
            }

            // Build a minimal no-DI logger for the demo
            return new AzureOpenAIGovernanceClient(
                endpoint, apiKey, deployment,
                new ConsoleLogger<AzureOpenAIGovernanceClient>());
        }

        // ── Reporting ─────────────────────────────────────────────────────────

        private static void PrintBanner()
        {
            Console.WriteLine(new string('━', 60));
            Console.WriteLine("  AgentOps Governance Demo");
            Console.WriteLine(new string('━', 60));
            Console.WriteLine();
        }

        private static void PrintReport(string agentFile, GovernanceReport report)
        {
            string statusEmoji = report.FinalStatus switch
            {
                "APPROVED" => "✅",
                "REVIEW"   => "⚠️",
                "BLOCKED"  => "❌",
                _          => "❓"
            };

            int exitCode = report.FinalStatus == "BLOCKED" ? 1 : 0;

            Console.WriteLine(new string('─', 60));
            Console.WriteLine($"  {statusEmoji} {report.AgentName} ({agentFile})");
            Console.WriteLine($"     Status      : {report.FinalStatus}");
            Console.WriteLine($"     Score       : {report.GovernanceScore}/100");
            Console.WriteLine($"     Criticals   : {report.CriticalViolations}");
            Console.WriteLine($"     Warnings    : {report.WarningViolations}");
            Console.WriteLine($"     Exit code   : {exitCode}");

            if (report.RuleResults.Exists(r => !r.IsCompliant))
            {
                Console.WriteLine("     Violations  :");
                foreach (var rule in report.RuleResults.FindAll(r => !r.IsCompliant))
                {
                    string icon = rule.Severity == RuleSeverity.Critical ? "🔴" : "🟡";
                    Console.WriteLine($"       {icon} {rule.RuleName}");
                    foreach (var v in rule.Violations)
                        Console.WriteLine($"          - {v}");
                }
            }

            var sem = report.SemanticAnalysis;
            Console.WriteLine("  🧠 Semantic Analysis:");
            if (sem == null || !sem.IsAvailable)
            {
                Console.WriteLine($"     Status: Skipped");
                if (sem?.ErrorMessage != null)
                    Console.WriteLine($"     Reason: {sem.ErrorMessage}");
            }
            else
            {
                string riskEmoji = sem.RiskLevel switch { "HIGH" => "🔴", "MEDIUM" => "🟡", _ => "🟢" };
                Console.WriteLine($"     Status    : Available");
                Console.WriteLine($"     Risk Level: {riskEmoji} {sem.RiskLevel}");
                if (sem.Issues.Count > 0)
                {
                    Console.WriteLine("     Issues:");
                    foreach (var i in sem.Issues) Console.WriteLine($"       - {i}");
                }
                if (sem.Recommendations.Count > 0)
                {
                    Console.WriteLine("     Recommendations:");
                    foreach (var r in sem.Recommendations) Console.WriteLine($"       - {r}");
                }
            }

            Console.WriteLine();
        }
    }
}
