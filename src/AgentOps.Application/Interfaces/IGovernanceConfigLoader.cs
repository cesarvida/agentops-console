using System.Threading.Tasks;
using AgentOps.Core.Governance;

namespace AgentOps.Application.Interfaces
{
    /// <summary>
    /// Loads the governance configuration for a GitHub repository.
    /// Implementations fetch <c>data/governance-config.yaml</c> from the repo;
    /// if absent, they return <see cref="GovernanceConfig.Default"/>.
    /// </summary>
    public interface IGovernanceConfigLoader
    {
        /// <summary>
        /// Loads the governance configuration for the specified repository.
        /// </summary>
        /// <param name="owner">Repository owner (user or organisation).</param>
        /// <param name="repo">Repository name.</param>
        /// <returns>
        /// The repo-specific <see cref="GovernanceConfig"/>, or
        /// <see cref="GovernanceConfig.Default"/> when none is found.
        /// </returns>
        Task<GovernanceConfig> LoadAsync(string owner, string repo);
    }
}
