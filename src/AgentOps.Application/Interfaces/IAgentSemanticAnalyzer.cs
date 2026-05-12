using System.Threading;
using System.Threading.Tasks;
using AgentOps.Core.Governance;

namespace AgentOps.Application.Interfaces
{
    /// <summary>
    /// Performs semantic governance analysis of an agent YAML definition
    /// using an LLM (e.g. Azure OpenAI).
    /// </summary>
    public interface IAgentSemanticAnalyzer
    {
        /// <summary>
        /// Analyzes the raw agent YAML and returns a semantic risk assessment.
        /// Must never throw — returns a <see cref="SemanticAnalysisResult.Skipped"/> result
        /// on any error so the rule-based governance check remains authoritative.
        /// </summary>
        /// <param name="agentYaml">Raw YAML content of the agent definition.</param>
        /// <param name="config">Semantic analysis settings (timeout, max tokens, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<SemanticAnalysisResult> AnalyzeAgentSemanticsAsync(
            string agentYaml,
            SemanticAnalysisConfig config,
            CancellationToken cancellationToken = default);
    }
}
