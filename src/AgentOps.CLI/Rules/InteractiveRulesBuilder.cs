using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using AgentOps.Core.Rules;
using AgentOps.Application.Rules;

namespace AgentOps.CLI.Rules
{
    /// <summary>
    /// Interactive rules builder that prompts the user for rules configuration.
    /// </summary>
    public class InteractiveRulesBuilder
    {
        private readonly IConsoleWriter _console;
        private readonly UserRulesLoader _loader;

        public InteractiveRulesBuilder(IConsoleWriter console, UserRulesLoader loader)
        {
            _console = console;
            _loader = loader;
        }

        /// <summary>
        /// Runs interactive mode to build rules from user input.
        /// </summary>
        public async Task<UserRules> BuildInteractiveAsync()
        {
            Console.WriteLine();
            _console.WriteLine("═══════════════════════════════════════════════════════════════");
            _console.WriteLine(" AgentOps — Configuración de Reglas");
            _console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine();

            var rules = new UserRules();

            // 1. Rule name
            _console.WriteLine("¿Qué nombre quieres darle a estas reglas?");
            rules.Name = PromptString("Mi set de reglas", "Mi set de reglas");

            Console.WriteLine();

            // 2. Allowed actions
            var defaults = UserRules.GetDefaults();
            _console.WriteLine("¿Acciones PERMITIDAS? (separadas por coma, Enter para usar defaults)");
            _console.WriteWarning($"Defaults: {string.Join(", ", defaults.AllowedActions)}");
            var allowedInput = PromptString("");
            rules.AllowedActions = allowedInput.Length > 0
                ? allowedInput.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList()
                : defaults.AllowedActions;

            Console.WriteLine();

            // 3. Forbidden actions
            _console.WriteLine("¿Acciones PROHIBIDAS? (separadas por coma, Enter para usar defaults)");
            _console.WriteWarning($"Defaults: {string.Join(", ", defaults.ForbiddenActions)}");
            var forbiddenInput = PromptString("");
            rules.ForbiddenActions = forbiddenInput.Length > 0
                ? forbiddenInput.Split(',').Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToList()
                : defaults.ForbiddenActions;

            Console.WriteLine();

            // 4. Owner required
            _console.WriteLine("¿El agente debe tener owner definido? (S/n)");
            rules.OwnerRequired = PromptYesNo(true);

            Console.WriteLine();

            // 5. Audit required
            _console.WriteLine("¿El agente debe tener audit logging? (S/n)");
            rules.AuditRequired = PromptYesNo(true);

            Console.WriteLine();

            // 6. Min score for APPROVED
            _console.WriteLine("¿Score mínimo para APPROVED? (default: 70)");
            rules.ReviewThreshold = PromptInt(70);

            Console.WriteLine();

            // 7. Min score to not be BLOCKED
            _console.WriteLine("¿Score mínimo para no ser BLOCKED? (default: 40)");
            rules.BlockedThreshold = PromptInt(40);

            Console.WriteLine();

            // 8. Save rules?
            _console.WriteLine("¿Guardar estas reglas para reutilizarlas? (S/n)");
            if (PromptYesNo(true))
            {
                _console.WriteLine("Nombre del archivo:");
                var fileName = PromptString("mis-reglas.yaml", "mis-reglas.yaml");
                await SaveRulesToFile(rules, fileName);
                _console.WriteSuccess($"✅ Reglas guardadas en data/rules/{fileName}");
            }

            Console.WriteLine();
            _console.WriteLine("Analizando agente con tus reglas...");
            Console.WriteLine();

            return rules;
        }

        // ── Helper methods ──────────────────────────────────────────────

        private string PromptString(string defaultValue = "", string suffix = "")
        {
            if (!string.IsNullOrEmpty(suffix))
            {
                Console.Write($"> ");
            }
            else
            {
                Console.Write($"> ");
            }

            var input = Console.ReadLine()?.Trim() ?? "";
            return !string.IsNullOrEmpty(input) ? input : defaultValue;
        }

        private bool PromptYesNo(bool defaultValue = true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(input))
                return defaultValue;

            return input == "s" || input == "si" || input == "yes" || input == "y";
        }

        private int PromptInt(int defaultValue = 0)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrEmpty(input))
                return defaultValue;

            if (int.TryParse(input, out var result))
                return result;

            _console.WriteWarning($"⚠ Entrada inválida, usando {defaultValue}");
            return defaultValue;
        }

        private async Task SaveRulesToFile(UserRules rules, string fileName)
        {
            var rulesDir = Path.Combine(Directory.GetCurrentDirectory(), "data", "rules");
            Directory.CreateDirectory(rulesDir);

            var filePath = Path.Combine(rulesDir, fileName);

            var rulesDict = new Dictionary<string, object>
            {
                {
                    "rules", new Dictionary<string, object>
                    {
                        { "name", rules.Name },
                        { "description", rules.Description },
                        {
                            "actions", new Dictionary<string, object>
                            {
                                { "allowed", rules.AllowedActions },
                                { "forbidden", rules.ForbiddenActions }
                            }
                        },
                        {
                            "requirements", new Dictionary<string, object>
                            {
                                { "owner_required", rules.OwnerRequired },
                                { "audit_required", rules.AuditRequired },
                                { "version_required", rules.VersionRequired }
                            }
                        },
                        {
                            "scoring", new Dictionary<string, object>
                            {
                                { "critical_penalty", rules.CriticalPenalty },
                                { "warning_penalty", rules.WarningPenalty },
                                { "blocked_threshold", rules.BlockedThreshold },
                                { "review_threshold", rules.ReviewThreshold }
                            }
                        }
                    }
                }
            };

            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(rulesDict);
            await File.WriteAllTextAsync(filePath, yaml);
        }
    }
}
