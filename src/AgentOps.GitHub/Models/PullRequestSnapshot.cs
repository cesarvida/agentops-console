namespace AgentOps.GitHub.Models;

/// <summary>
/// Snapshot of a pull request at the moment of analysis.
/// Captures PR metadata, diff content, and modified files for auditable, reproducible analysis.
/// </summary>
public sealed record PullRequestSnapshot
{
    public required string Owner { get; init; }
    public required string Repository { get; init; }
    public required int Number { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Author { get; init; }
    public required string State { get; init; }
    public required string BaseBranch { get; init; }
    public required string HeadBranch { get; init; }
    public required int CommitCount { get; init; }
    public required List<DiffFile> Files { get; init; }
    public required DateTime SnapshotTimestamp { get; init; }
    public required string Url { get; init; }
}
