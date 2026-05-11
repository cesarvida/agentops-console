using System.Threading.Tasks;

namespace AgentOps.Application.Interfaces
{
    /// <summary>
    /// Interface for semantic code analysis via LLM (e.g., Azure OpenAI).
    /// </summary>
    public interface ILLMClient
    {
        /// <summary>
        /// Analyze a code diff for semantic quality issues (security, style, architecture, etc.)
        /// </summary>
        /// <param name="diff">The unified diff or code changes to analyze</param>
        /// <param name="context">Optional context about the PR (title, description, etc.)</param>
        /// <returns>JSON string with semantic findings (issues, recommendations)</returns>
        Task<string> AnalyzeCodeAsync(string diff, string context);
    }
}
