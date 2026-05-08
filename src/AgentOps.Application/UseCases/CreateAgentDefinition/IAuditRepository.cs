using System.Threading.Tasks;
using AgentOps.Application.UseCases.CreateAgentDefinition;

namespace AgentOps.Application.UseCases.CreateAgentDefinition
{
    public interface IAuditRepository
    {
        Task<string> AppendAsync(AuditEntry entry);
    }
}
