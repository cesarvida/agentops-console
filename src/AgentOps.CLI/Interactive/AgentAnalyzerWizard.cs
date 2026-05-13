using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AgentOps.Application.Governance;
using AgentOps.Application.Interfaces;
using AgentOps.Application.Rules;
using AgentOps.CLI.Rules;
using AgentOps.Core.Entities;
using AgentOps.Core.Governance;
using AgentOps.Core.Rules;
using Microsoft.Extensions.DependencyInjection;

namespace AgentOps.CLI.Interactive
{
    /// <summary>
    /// Interactive wizard that guides users step-by-step through analyzing an agent.
    /// </summary>
    public class AgentAnalyzerWizard
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAgentSemanticAnalyzer _semanticAnalyzer;
        private AgentDefinition? _currentAgent;
        private GovernanceConfig? _currentConfig;

        public AgentAnalyzerWizard(
            IServiceProvider serviceProvider,
            IAgentSemanticAnalyzer semanticAnalyzer)
        {
            _serviceProvider = serviceProvider;
            _semanticAnalyzer = semanticAnalyzer;
        }

        public async Task RunAsync()
        {
            while (true)
            {
                PrintHeader("🔍 Analizador de Agentes");

                // Step 1: Load Agent
                var (agent, filePath) = await Step1_LoadAgentAsync();
                if (agent == null) return; // User cancelled

                _currentAgent = agent;

                // Step 2: Choose Rules
                var config = await Step2_ChooseRulesAsync();
                if (config == null) return; // User cancelled

                _currentConfig = config;

                // Step 3: Run and Show Result
                await Step3_RunAndShowAsync(agent, filePath, config);

                // Step 4: What Next?
                var nextAction = await Step4_WhatNextAsync();
                if (nextAction == NextAction.MainMenu || nextAction == NextAction.Exit)
                    return;
            }
        }

        private async Task<(AgentDefinition?, string?)> Step1_LoadAgentAsync()
        {
            while (true)
            {
                PrintStepHeader("PASO 1: CARGAR EL AGENTE");
                Console.WriteLine();
                Console.WriteLine("¿Dónde está el agente que quieres analizar?");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  → data/agent-definitions/compliant-agent.yaml");
                Console.WriteLine("  → ./mi-agente.json");
                Console.WriteLine("  → https://raw.githubusercontent.com/owner/repo/path/agent.yaml");
                Console.ResetColor();
                Console.WriteLine();
                Console.Write("Ruta o URL del agente: ");

                string? filePath = Console.ReadLine();
                if (filePath == null) return (null, null);

                filePath = filePath.Trim();
                if (string.IsNullOrEmpty(filePath))
                {
                    PrintError("La ruta no puede estar vacía.");
                    Console.WriteLine();
                    continue;
                }

                try
                {
                    AgentDefinition? agent = null;
                    
                    // Detectar si es URL o ruta local
                    if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        // Cargar desde URL
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine($"📥 Descargando agente desde GitHub...");
                        Console.ResetColor();
                        
                        agent = await AgentDefinitionLoader.LoadFromUrlAsync(filePath);
                    }
                    else
                    {
                        // Cargar desde archivo local
                        var (loadedAgent, _) = await AgentDefinitionLoader.LoadWithContextAsync(filePath);
                        agent = loadedAgent;
                    }

                    if (agent == null)
                    {
                        PrintError("No se pudo cargar el agente.");
                        Console.WriteLine();
                        continue;
                    }

                    Console.WriteLine();
                    PrintSuccess($"✅ Agente cargado: {agent.Name} v{agent.Version}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"   Formato: {(filePath.EndsWith(".json") ? "JSON" : "YAML")}");

                    if (agent.Configuration?.Owner != null)
                        Console.WriteLine($"   Owner: {agent.Configuration.Owner}");

                    if (agent.Tools?.Count > 0)
                        Console.WriteLine($"   Tools: {agent.Tools.Count}");

                    Console.ResetColor();
                    Console.WriteLine();
                    return (agent, filePath);
                }
                catch (Exception ex)
                {
                    PrintError($"Error: {ex.Message}");
                    Console.WriteLine();
                }
            }
        }

        private async Task<GovernanceConfig?> Step2_ChooseRulesAsync()
        {
            PrintStepHeader("PASO 2: ELEGIR LAS REGLAS");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("¿Qué reglas quieres usar?");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("  1) Reglas por defecto");
                Console.WriteLine("  2) Reglas estrictas");
                Console.WriteLine("  3) Reglas relajadas");
                Console.WriteLine("  4) Cargar mi archivo");
                Console.WriteLine("  5) Definir ahora");
                Console.ResetColor();
                Console.WriteLine();
                Console.Write("Elige [1-5]: ");

                string? choice = Console.ReadLine();
                if (choice == null) return null;

                switch (choice.Trim())
                {
                    case "1":
                        return await LoadPresetRulesAsync("data/rules/default-rules.yaml");
                    case "2":
                        return await LoadPresetRulesAsync("data/rules/strict-rules.yaml");
                    case "3":
                        return await LoadPresetRulesAsync("data/rules/relaxed-rules.yaml");
                    case "4":
                        return await Step2_LoadUserFileAsync();
                    case "5":
                        return await Step2_DefineNowAsync();
                    default:
                        PrintError("Opción no válida.");
                        Console.WriteLine();
                        continue;
                }
            }
        }

        private async Task<GovernanceConfig?> Step2_LoadUserFileAsync()
        {
            Console.WriteLine();
            PrintStepHeader("CARGAR ARCHIVO DE REGLAS");
            Console.WriteLine();

            var ruleDir = "data/rules";
            if (!Directory.Exists(ruleDir))
            {
                PrintError($"Carpeta {ruleDir} no encontrada.");
                Console.WriteLine();
                return null;
            }

            var ruleFiles = Directory.GetFiles(ruleDir, "*.yaml")
                .Select(p => new FileInfo(p))
                .OrderBy(f => f.Name)
                .ToList();

            if (ruleFiles.Count == 0)
            {
                PrintError("No hay archivos de reglas.");
                Console.WriteLine();
                return null;
            }

            Console.WriteLine("Archivos disponibles:");
            Console.WriteLine();

            for (int i = 0; i < ruleFiles.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  {i + 1}) {ruleFiles[i].Name}");
                Console.ResetColor();
            }

            Console.WriteLine($"  {ruleFiles.Count + 1}) Otra ruta");
            Console.WriteLine();
            Console.Write($"Elige [1-{ruleFiles.Count + 1}]: ");

            string? choice = Console.ReadLine();
            if (choice == null) return null;

            if (int.TryParse(choice.Trim(), out int idx) && idx > 0 && idx <= ruleFiles.Count)
            {
                var selectedFile = Path.Combine(ruleDir, ruleFiles[idx - 1].Name);
                return await LoadPresetRulesAsync(selectedFile);
            }
            else if (choice.Trim() == (ruleFiles.Count + 1).ToString())
            {
                Console.WriteLine();
                Console.Write("Ruta: ");
                string? customPath = Console.ReadLine();
                if (customPath == null) return null;
                return await LoadPresetRulesAsync(customPath.Trim());
            }

            PrintError("Opción no válida.");
            Console.WriteLine();
            return null;
        }

        private async Task<GovernanceConfig?> LoadPresetRulesAsync(string filePath)
        {
            try
            {
                var loader = _serviceProvider.GetRequiredService<UserRulesLoader>();
                var userRules = await loader.LoadFromFileAsync(filePath);
                Console.WriteLine();
                PrintSuccess($"✅ Reglas cargadas");
                return userRules.ToGovernanceConfig();
            }
            catch (Exception ex)
            {
                PrintError($"Error: {ex.Message}");
                Console.WriteLine();
                return null;
            }
        }

        private async Task<GovernanceConfig?> Step2_DefineNowAsync()
        {
            Console.WriteLine();
            PrintStepHeader("DEFINIR REGLAS");
            Console.WriteLine();

            var builder = _serviceProvider.GetRequiredService<InteractiveRulesBuilder>();
            try
            {
                var userRules = await builder.BuildInteractiveAsync();
                Console.WriteLine();
                PrintSuccess("✅ Reglas configuradas");
                return userRules.ToGovernanceConfig();
            }
            catch
            {
                return null;
            }
        }

        private async Task Step3_RunAndShowAsync(
            AgentDefinition agent,
            string filePath,
            GovernanceConfig config)
        {
            PrintStepHeader("PASO 3: EJECUTAR ANÁLISIS");
            Console.WriteLine();
            Console.WriteLine($"Analizando {agent.Name} v{agent.Version}...");
            Console.WriteLine();

            try
            {
                var ruleEngine = _serviceProvider.GetRequiredService<GovernanceRuleEngine>();
                var report = await ruleEngine.EvaluateAsync(agent, config);
                var semanticResult = report.SemanticAnalysis ?? new SemanticAnalysisResult { IsAvailable = false };
                PrintAnalysisResult(agent, report, semanticResult);
            }
            catch (Exception ex)
            {
                PrintError($"Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        private void PrintAnalysisResult(
            AgentDefinition agent,
            GovernanceReport report,
            SemanticAnalysisResult semanticResult)
        {
            PrintBox("RESULTADO DEL ANÁLISIS", ConsoleColor.Cyan);
            Console.WriteLine();

            Console.Write("║  Agente:  ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(agent.Name);
            Console.ResetColor();

            Console.Write("║  Versión: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(agent.Version);
            Console.ResetColor();

            if (agent.Configuration?.Owner != null)
            {
                Console.Write("║  Owner:   ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(agent.Configuration.Owner);
                Console.ResetColor();
            }

            PrintDivider();
            Console.WriteLine("║  REGLAS:");

            foreach (var result in report.RuleResults ?? new List<RuleResult>())
            {
                var icon = result.Severity == RuleSeverity.Critical ? "❌" :
                          result.Severity == RuleSeverity.Warning ? "⚠️ " : "✅";
                var color = result.Severity == RuleSeverity.Critical ? ConsoleColor.Red :
                           result.Severity == RuleSeverity.Warning ? ConsoleColor.Yellow : ConsoleColor.Green;

                Console.ForegroundColor = color;
                Console.WriteLine($"║  {icon} {result.RuleName}");
                Console.ResetColor();
            }

            if (semanticResult.IsAvailable)
            {
                PrintDivider();
                Console.Write("║  🧠 Semántico: ");
                Console.ForegroundColor = GetRiskColor(semanticResult.RiskLevel);
                Console.WriteLine(semanticResult.RiskLevel);
                Console.ResetColor();
            }

            PrintDivider();

            Console.Write("║  SCORE: ");
            Console.ForegroundColor = GetScoreColor(report.GovernanceScore);
            Console.WriteLine($"{report.GovernanceScore}/100");
            Console.ResetColor();

            Console.Write("║  ESTADO: ");
            var statusColor = report.FinalStatus == "APPROVED" ? ConsoleColor.Green :
                             report.FinalStatus == "REVIEW" ? ConsoleColor.Yellow : ConsoleColor.Red;
            Console.ForegroundColor = statusColor;
            Console.WriteLine($"{report.FinalStatus}");
            Console.ResetColor();

            PrintBoxEnd();
        }

        private async Task<NextAction> Step4_WhatNextAsync()
        {
            Console.WriteLine("¿Qué quieres hacer?");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  1) Analizar otro agente");
            Console.WriteLine("  2) Cambiar reglas y re-analizar");
            Console.WriteLine("  3) Guardar reporte");
            Console.WriteLine("  4) Menú principal");
            Console.ResetColor();
            Console.WriteLine();
            Console.Write("Elige [1-4]: ");

            string? choice = Console.ReadLine();
            if (choice == null) return NextAction.Exit;

            switch (choice.Trim())
            {
                case "1":
                    Console.WriteLine();
                    return NextAction.AnalyzeAnother;
                case "2":
                    Console.WriteLine();
                    return NextAction.ChangeRulesAndReanalyze;
                case "3":
                    if (_currentAgent != null)
                        await SaveReportAsync(_currentAgent);
                    Console.WriteLine();
                    return NextAction.AnalyzeAnother;
                case "4":
                    Console.WriteLine();
                    return NextAction.MainMenu;
                default:
                    return await Step4_WhatNextAsync();
            }
        }

        private async Task SaveReportAsync(AgentDefinition agent)
        {
            try
            {
                var filename = $"report-{agent.Name}-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                Directory.CreateDirectory("outputs");
                PrintSuccess($"✅ Reporte guardado: outputs/{filename}");
            }
            catch (Exception ex)
            {
                PrintError($"Error: {ex.Message}");
            }
        }

        private void PrintHeader(string title)
        {
            Console.WriteLine();
            PrintBox(title, ConsoleColor.Cyan);
            Console.WriteLine();
        }

        private void PrintStepHeader(string step)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("─────────────────────────────────────────");
            Console.WriteLine(step);
            Console.WriteLine("─────────────────────────────────────────");
            Console.ResetColor();
            Console.WriteLine();
        }

        private void PrintBox(string title, ConsoleColor color)
        {
            var width = 50;
            Console.ForegroundColor = color;
            Console.WriteLine(new string('═', width));
            Console.WriteLine($"║ {title.PadRight(width - 4)} ║");
            Console.WriteLine(new string('═', width));
            Console.ResetColor();
        }

        private void PrintBoxEnd()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(new string('═', 58));
            Console.ResetColor();
        }

        private void PrintDivider()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
            Console.ResetColor();
        }

        private void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ {message}");
            Console.ResetColor();
        }

        private ConsoleColor GetScoreColor(int score)
        {
            return score >= 70 ? ConsoleColor.Green :
                   score >= 50 ? ConsoleColor.Yellow : ConsoleColor.Red;
        }

        private ConsoleColor GetRiskColor(string? riskLevel)
        {
            return riskLevel switch
            {
                "HIGH" => ConsoleColor.Red,
                "MEDIUM" => ConsoleColor.Yellow,
                "LOW" => ConsoleColor.Green,
                _ => ConsoleColor.Gray
            };
        }

        private enum NextAction
        {
            AnalyzeAnother,
            ChangeRulesAndReanalyze,
            SaveReport,
            MainMenu,
            Exit
        }
    }
}
