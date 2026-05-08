using System;
using System.Threading.Tasks;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using AgentOps.Core.Exceptions;
using AgentOps.Application.UseCases.CreateAgentDefinition;

namespace AgentOps.Application.UseCases.CreateAgentDefinition
{
    public class CreateAgentDefinitionHandler
    {
        private readonly IAgentDefinitionRepository _repository;
        private readonly IAuditRepository _auditRepository;
        private readonly IClock _clock;

        public CreateAgentDefinitionHandler(
            IAgentDefinitionRepository repository,
            IAuditRepository auditRepository,
            IClock clock)
        {
            _repository = repository;
            _auditRepository = auditRepository;
            _clock = clock;
        }

        public async Task<CreateAgentDefinitionResponse> HandleAsync(CreateAgentDefinitionRequest request)
        {
            // Guard clauses
            if (request == null)
                return new CreateAgentDefinitionResponse { Status = "Error", Errors = "Request is null" };
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Description) || string.IsNullOrWhiteSpace(request.Purpose))
                return new CreateAgentDefinitionResponse { Status = "Error", Errors = "Name, Description, and Purpose are required" };
            if (request.Rules == null || request.Rules.Count == 0)
                return new CreateAgentDefinitionResponse { Status = "Error", Errors = "At least one rule is required" };
            if (request.Tools == null || request.Tools.Count == 0)
                return new CreateAgentDefinitionResponse { Status = "Error", Errors = "At least one tool is required" };

            var agentId = new AgentId(Guid.NewGuid().ToString());
            var createdAt = _clock.UtcNow;
            var version = "1.0";
            AgentDefinition agent;
            try
            {
                agent = new AgentDefinition(
                    agentId,
                    request.Name,
                    request.Description,
                    request.Purpose,
                    request.Rules,
                    request.Tools,
                    request.Configuration ?? new AgentConfiguration(),
                    createdAt,
                    version
                );
            }
            catch (InvalidAgentDefinitionException ex)
            {
                return new CreateAgentDefinitionResponse { Status = "Error", Errors = ex.Message };
            }

            var savedPath = await _repository.AddAsync(agent);
            var auditId = await _auditRepository.AppendAsync(new AuditEntry
            {
                TimestampUtc = createdAt,
                Action = "CreateAgentDefinition",
                EntityType = "AgentDefinition",
                EntityId = agentId.Value,
                Status = "Success",
                Details = $"Agent '{agent.Name}' created."
            });

            return new CreateAgentDefinitionResponse
            {
                AgentId = agentId.Value,
                SavedPath = savedPath,
                AuditId = auditId,
                Status = "Success"
            };
        }
    }
}
