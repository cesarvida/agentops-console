using AgentOps.GitHub;
using Microsoft.Extensions.Logging;

namespace AgentOps.CLI.Commands;

/// <summary>
/// CLI command to analyze GitHub pull requests.
/// </summary>
public sealed class AnalyzePullRequestCommand
{
    private readonly IGitHubPullRequestClient _githubClient;
    private readonly ILogger<AnalyzePullRequestCommand> _logger;

    public AnalyzePullRequestCommand(
        IGitHubPullRequestClient githubClient,
        ILogger<AnalyzePullRequestCommand> logger)
    {
        _githubClient = githubClient ?? throw new ArgumentNullException(nameof(githubClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(string? owner = null, string? repo = null, int? prNumber = null)
    {
        try
        {
            owner ??= PromptForInput("Enter repository owner: ");
            repo ??= PromptForInput("Enter repository name: ");

            if (!int.TryParse(prNumber?.ToString() ?? PromptForInput("Enter PR number: "), out var pr) || pr <= 0)
            {
                Console.WriteLine("❌ Invalid PR number");
                return;
            }

            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("❌ GITHUB_TOKEN not set. Use: $env:GITHUB_TOKEN='your_token'");
                return;
            }

            Console.WriteLine($"\n📡 Fetching PR #{pr} from {owner}/{repo}...");
            var snapshot = await _githubClient.GetPullRequestAsync(owner, repo, pr);

            DisplaySnapshot(snapshot);
            Console.WriteLine($"\n✅ PR analysis complete. Snapshot: {snapshot.SnapshotTimestamp:O}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"❌ Authentication Error: {ex.Message}");
            _logger.LogError(ex, "Auth failed");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ {ex.Message}");
            _logger.LogWarning(ex, "Invalid PR request");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            _logger.LogError(ex, "Unexpected error");
        }
    }

    private static string PromptForInput(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? string.Empty;
    }

    private static void DisplaySnapshot(AgentOps.GitHub.Models.PullRequestSnapshot snapshot)
    {
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine($"📋 PR #{snapshot.Number}: {snapshot.Title}");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"Author: {snapshot.Author} | State: {snapshot.State}");
        Console.WriteLine($"Base: {snapshot.BaseBranch} ← Head: {snapshot.HeadBranch}");
        Console.WriteLine($"Commits: {snapshot.CommitCount} | Files: {snapshot.Files.Count}");
        Console.WriteLine($"Changes: +{snapshot.Files.Sum(f => f.Additions)} -{snapshot.Files.Sum(f => f.Deletions)}");
        Console.WriteLine();

        foreach (var file in snapshot.Files.Take(10))
        {
            var symbol = file.Status switch
            {
                "added" => "➕",
                "removed" => "➖",
                "modified" => "📝",
                "renamed" => "🔄",
                _ => "❓"
            };
            Console.WriteLine($"{symbol} {file.Path} (+{file.Additions} -{file.Deletions})");
        }

        if (snapshot.Files.Count > 10)
            Console.WriteLine($"... and {snapshot.Files.Count - 10} more files");
    }
}
