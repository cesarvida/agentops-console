using AgentOps.Application.Analysis;
using AgentOps.Application.Interfaces;
using AgentOps.GitHub;
using Microsoft.Extensions.Logging;

namespace AgentOps.CLI.Commands;

public sealed class AnalyzePullRequestCommand
{
    private readonly IGitHubPullRequestClient _githubClient;
    private readonly PromptAnalyzer _analyzer;
    private readonly ICommentPoster _commentPoster;
    private readonly ILogger<AnalyzePullRequestCommand> _logger;

    public AnalyzePullRequestCommand(
        IGitHubPullRequestClient githubClient,
        PromptAnalyzer analyzer,
        ICommentPoster commentPoster,
        ILogger<AnalyzePullRequestCommand> logger)
    {
        _githubClient = githubClient;
        _analyzer = analyzer;
        _commentPoster = commentPoster;
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(string owner, string repo, int prNumber)
    {
        Console.WriteLine($"\n🔍 Analyzing PR #{prNumber} in {owner}/{repo}...");
        Console.WriteLine(new string('-', 50));

        var snapshot = await _githubClient.GetPullRequestAsync(owner, repo, prNumber);
        var relevantFiles = snapshot.Files
            .Select(f => f.Path)
            .Where(f => f.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!relevantFiles.Any())
        {
            Console.WriteLine("No .md or .py files changed in this PR.");
            return 0;
        }

        Console.WriteLine($"Found {relevantFiles.Count} file(s) to analyze:");
        foreach (var f in relevantFiles) Console.WriteLine($"  - {f}");
        Console.WriteLine();

        // Create evaluation output directory
        var evaluationDir = "data/evaluations";
        Directory.CreateDirectory(evaluationDir);

        int maxExitCode = 0;
        foreach (var filePath in relevantFiles)
        {
            if (!File.Exists(filePath)) { Console.WriteLine($"Skipping {filePath} (not found locally)"); continue; }
            var report = await _analyzer.AnalyzeAsync(filePath);
            Console.WriteLine($"{report.FileName}  ->  {report.Decision}  ({report.RiskScore}/100)");
            
            // Save evaluation report to data/evaluations/
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var evaluationPath = Path.Combine(evaluationDir, $"evaluation_{fileName}_{DateTime.UtcNow:yyyyMMddHHmmss}.json");
            await File.WriteAllTextAsync(evaluationPath, report.ToAuditJson());
            Console.WriteLine($"  📄 Saved to {evaluationPath}");
            
            try { await _commentPoster.PostPromptAnalysisAsync(owner, repo, prNumber, report); }
            catch (Exception ex) { _logger.LogWarning("Failed to post comment: {Message}", ex.Message); }
            if (report.Decision == "BLOCK") maxExitCode = 1;
        }
        Console.WriteLine();
        return maxExitCode;
    }
}
