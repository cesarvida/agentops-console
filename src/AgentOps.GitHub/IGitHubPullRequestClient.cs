namespace AgentOps.GitHub;

using AgentOps.GitHub.Models;

/// <summary>
/// Interface for GitHub API interactions, specifically for reading pull requests.
/// Abstraction layer for Octokit.NET to enable testability and dependency injection.
/// </summary>
public interface IGitHubPullRequestClient
{
    /// <summary>
    /// Retrieves pull request snapshot with diff and metadata from GitHub.
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <param name="prNumber">Pull request number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PullRequestSnapshot containing PR data, diff, and modified files</returns>
    Task<PullRequestSnapshot> GetPullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        CancellationToken cancellationToken = default);
}
