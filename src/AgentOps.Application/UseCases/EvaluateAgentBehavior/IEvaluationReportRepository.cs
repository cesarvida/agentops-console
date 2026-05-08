using System.Threading.Tasks;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior
{
    /// <summary>Result returned after persisting a redacted evaluation report.</summary>
    public sealed class SaveReportResult
    {
        public string StoragePath { get; init; } = string.Empty;
        public string ArtifactId  { get; init; } = string.Empty;
        /// <summary>SHA-256 hex digest (64 chars) of the bytes actually written to disk.</summary>
        public string ArtifactDigest { get; init; } = string.Empty;
    }

    public interface IEvaluationReportRepository
    {
        Task<SaveReportResult> SaveReportAsync(string evaluationId, EvaluationReport report);
    }
}
