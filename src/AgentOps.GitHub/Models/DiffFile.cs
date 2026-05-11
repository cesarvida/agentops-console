namespace AgentOps.GitHub.Models;

/// <summary>
/// Represents a single file changed in a pull request.
/// </summary>
public sealed record DiffFile
{
    public required string Path { get; init; }
    public required string Status { get; init; }
    public required int Additions { get; init; }
    public required int Deletions { get; init; }
    public required int Changes { get; init; }
    public required string Patch { get; init; }
    public string? PreviousPath { get; init; }
    public required bool IsBinary { get; init; }
}
