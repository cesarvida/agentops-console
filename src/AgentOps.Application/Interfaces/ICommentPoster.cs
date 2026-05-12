using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using AgentOps.Core.Governance;

namespace AgentOps.Application.Interfaces
{
    /// <summary>
    /// Interface for posting analysis comments to external platforms (e.g., GitHub).
    /// </summary>
    public interface ICommentPoster
    {
        /// <summary>
        /// Posts an analysis comment about PR findings to an external system.
        /// </summary>
        Task PostAnalysisCommentAsync(string owner, string repo, int prNumber, EvaluationReport report);

        /// <summary>
        /// Posts a governance validation report as a PR comment.
        /// </summary>
        Task PostGovernanceReportAsync(string owner, string repo, int prNumber, GovernanceReport report);
    }
}
