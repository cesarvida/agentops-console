using System.Collections.Generic;
using AgentOps.Core.Entities;

namespace AgentOps.Application.UseCases.CreateAgentDefinition
{
    public class CreateAgentDefinitionRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Purpose { get; set; }
        public List<string> Rules { get; set; }
        public List<string> Tools { get; set; }
        public AgentConfiguration? Configuration { get; set; }
    }
}
