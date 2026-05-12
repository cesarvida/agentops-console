using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.Core.Entities;

namespace AgentOps.Application.Interfaces
{
    /// <summary>
    /// Fetches agent definitions from a source (e.g., a GitHub repository).
    /// </summary>
    public interface IAgentFetcher
    {
        /// <summary>
        /// Fetches all agent YAML definitions from the data/agent-definitions directory
        /// of the specified GitHub repository.
        /// </summary>
        /// <param name="owner">Repository owner (user or org).</param>
        /// <param name="repo">Repository name.</param>
        /// <returns>List of deserialized AgentDefinition objects; empty if the directory does not exist.</returns>
        Task<List<AgentDefinition>> FetchAgentsAsync(string owner, string repo);
    }
}
