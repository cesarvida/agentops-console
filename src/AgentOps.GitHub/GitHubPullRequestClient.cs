using Octokit;
using AgentOps.GitHub.Models;

namespace AgentOps.GitHub;

/// <summary>
/// GitHub API client for reading pull request data using Octokit.NET
/// </summary>
public sealed class GitHubPullRequestClient : IGitHubPullRequestClient
{
    private readonly IGitHubClient _client;

    public GitHubPullRequestClient(string gitHubToken)
    {
        if (string.IsNullOrWhiteSpace(gitHubToken))
            throw new ArgumentException("GitHub token cannot be null or empty", nameof(gitHubToken));

        var credentials = new Credentials(gitHubToken);
        _client = new GitHubClient(new ProductHeaderValue("AgentOps-Console"))
        {
            Credentials = credentials
        };
    }

    public async Task<PullRequestSnapshot> GetPullRequestAsync(
        string owner,
        string repo,
        int prNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Owner cannot be null or empty", nameof(owner));
        if (string.IsNullOrWhiteSpace(repo))
            throw new ArgumentException("Repository name cannot be null or empty", nameof(repo));
        if (prNumber <= 0)
            throw new ArgumentException("PR number must be greater than 0", nameof(prNumber));

        try
        {
            var pullRequest = await _client.PullRequest.Get(owner, repo, prNumber);
            var files = await _client.PullRequest.Files(owner, repo, prNumber);

            var diffFiles = files.Select(file => new DiffFile
            {
                Path = file.FileName,
                Status = file.Status,
                Additions = file.Additions,
                Deletions = file.Deletions,
                Changes = file.Changes,
                Patch = file.Patch ?? string.Empty,
                PreviousPath = file.PreviousFileName,
                IsBinary = file.Patch == null
            }).ToList();

            var snapshot = new PullRequestSnapshot
            {
                Owner = owner,
                Repository = repo,
                Number = prNumber,
                Title = pullRequest.Title,
                Description = pullRequest.Body ?? string.Empty,
                Author = pullRequest.User.Login,
                State = pullRequest.State.StringValue,
                BaseBranch = pullRequest.Base.Ref,
                HeadBranch = pullRequest.Head.Ref,
                CommitCount = pullRequest.Commits,
                Files = diffFiles,
                SnapshotTimestamp = DateTime.UtcNow,
                Url = pullRequest.HtmlUrl
            };

            return snapshot;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("404"))
        {
            throw new InvalidOperationException(
                $"Pull request #{prNumber} not found in {owner}/{repo}", ex);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("401") || ex.Message.Contains("403"))
        {
            throw new UnauthorizedAccessException(
                $"Authentication failed for {owner}/{repo}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"GitHub API error: {ex.Message}", ex);
        }
    }
}
