using System;

namespace AgentOps.Application.UseCases.CreateAgentDefinition
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
