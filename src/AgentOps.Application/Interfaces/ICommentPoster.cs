using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.Application.Interfaces
{
    /// <summary>
    /// Interface for posting analysis comments to external platforms (e.g., GitHub).
    /// </summary>
    public interface ICommentPoster
    {
        /// <summary>
        /// Posts an evaluation analysis comment about PR findings to an external system.
        /// </summary>
        Task PostAnalysisCommentAsync(string owner, string repo, int prNumber, EvaluationReport report);

        /// <summary>
        /// Posts a prompt security analysis report as a PR comment.
        /// </summary>
        Task PostPromptAnalysisAsync(string owner, string repo, int prNumber, PromptFileSafetyReport report);
    }
}
