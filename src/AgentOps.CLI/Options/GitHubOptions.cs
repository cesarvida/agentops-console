namespace AgentOps.CLI.Options;

/// <summary>
/// Configuration for GitHub PR analysis. GITHUB_TOKEN read from environment.
/// </summary>
public sealed class GitHubOptions
{
    public string GitHubToken { get; set; } = 
        Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? string.Empty;

    public required string Owner { get; init; }
    public required string Repository { get; init; }
    public required int PullRequestNumber { get; init; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(GitHubToken))
            throw new InvalidOperationException("GITHUB_TOKEN environment variable not set");

        if (string.IsNullOrWhiteSpace(Owner))
            throw new InvalidOperationException("Owner cannot be empty");

        if (string.IsNullOrWhiteSpace(Repository))
            throw new InvalidOperationException("Repository cannot be empty");

        if (PullRequestNumber <= 0)
            throw new InvalidOperationException("Pull request number must be > 0");
    }
}
