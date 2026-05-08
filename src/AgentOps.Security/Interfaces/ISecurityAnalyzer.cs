using AgentOps.Core.Entities;
using AgentOps.Security.Models;

namespace AgentOps.Security.Interfaces
{
    public interface ISecurityAnalyzer
    {
        SecurityAnalysisResult Analyze(AgentDefinition agent);
    }
}
