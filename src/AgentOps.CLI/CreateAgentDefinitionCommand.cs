using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Core.Entities;

namespace AgentOps.CLI
{
    public class CreateAgentDefinitionCommand
    {
        private readonly CreateAgentDefinitionHandler _handler;
        private readonly IConsoleWriter _console;

        public CreateAgentDefinitionCommand(CreateAgentDefinitionHandler handler, IConsoleWriter console)
        {
            _handler = handler;
            _console = console;
        }

        public async Task ExecuteAsync()
        {
            _console.WriteLine("=== Create New Agent Definition [MVP] ===");
            var req = new CreateAgentDefinitionRequest();
            req.Name = Prompt("Name (3-100 chars)");
            req.Description = Prompt("Description (min 10 chars)");
            req.Purpose = Prompt("Purpose");
            req.Rules = PromptList("Rules (comma-separated, at least 1)");
            req.Tools = PromptList("Tools (comma-separated, at least 1)");
            req.Configuration = null; // Optional for MVP

            _console.WriteLine("\nSummary:");
            _console.WriteLine($"Name: {req.Name}");
            _console.WriteLine($"Description: {req.Description}");
            _console.WriteLine($"Purpose: {req.Purpose}");
            _console.WriteLine($"Rules: {string.Join(", ", req.Rules)}");
            _console.WriteLine($"Tools: {string.Join(", ", req.Tools)}");

            var confirm = Prompt("Proceed? (yes/no)");
            if (!confirm.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                _console.WriteWarning("Operation cancelled.");
                return;
            }

            var response = await _handler.HandleAsync(req);
            if (response.Status == "Success")
            {
                _console.WriteSuccess($"Agent created! Id: {response.AgentId}");
                _console.WriteLine($"Saved at: {response.SavedPath}");
                _console.WriteLine($"AuditId: {response.AuditId}");
            }
            else
            {
                _console.WriteError($"Error: {response.Errors}");
            }
        }

        private string Prompt(string label)
        {
            _console.WriteLine(label + ": ");
            return Console.ReadLine() ?? string.Empty;
        }

        private List<string> PromptList(string label)
        {
            _console.WriteLine(label + ": ");
            var input = Console.ReadLine() ?? string.Empty;
            var items = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>();
            foreach (var item in items)
            {
                var trimmed = item.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                    list.Add(trimmed);
            }
            return list;
        }
    }
}
