using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Core.Entities;

namespace AgentOps.CLI
{
    public class ListAgentsCommand
    {
        private readonly IAgentDefinitionRepository _repo;
        private readonly IConsoleWriter _console;

        public ListAgentsCommand(IAgentDefinitionRepository repo, IConsoleWriter console)
        {
            _repo = repo;
            _console = console;
        }

        public async Task ExecuteAsync()
        {
            _console.WriteLine("=== Agent Definitions ===");
            var agents = (await _repo.ListAllAsync())?.ToList() ?? new List<AgentDefinition>();

            if (!agents.Any())
            {
                _console.WriteLine("No agent definitions found.");
                return;
            }

            var index = 1;
            foreach (var a in agents)
            {
                _console.WriteLine($"{index++}. {a.Name}  (Id: {a.Id.Value})");
                _console.WriteLine($"   Purpose: {a.Purpose}");
                var rules = a.Rules ?? new List<string>();
                _console.WriteLine($"   Rules: {string.Join(", ", rules)}");
            }
        }
    }
}
