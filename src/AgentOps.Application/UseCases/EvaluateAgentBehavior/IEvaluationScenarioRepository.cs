using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior
{
    public interface IEvaluationScenarioRepository
    {
        Task<EvaluationScenario?> GetByIdAsync(string scenarioId);
    }
}
