using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.GitHub;
using AgentOps.Application.UseCases.EvaluateAgentBehavior;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Core.Entities;
using AgentOps.Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AgentOps.CLI.Commands;

/// <summary>
/// CLI command to analyze GitHub pull requests and connect to analyzers.
/// </summary>
public sealed class AnalyzePullRequestCommand
{
    private readonly IGitHubPullRequestClient _githubClient;
    private readonly ILogger<AnalyzePullRequestCommand> _logger;
    private readonly EvaluateAgentBehaviorHandler _evaluator;
    private readonly IAgentDefinitionRepository _agentRepo;
    private readonly IAuditRepository _auditRepo;
    private readonly AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators.SemanticCodeAnalyzer _semanticAnalyzer;

    public AnalyzePullRequestCommand(
        IGitHubPullRequestClient githubClient,
        ILogger<AnalyzePullRequestCommand> logger,
        EvaluateAgentBehaviorHandler evaluator,
        IAgentDefinitionRepository agentRepo,
        IAuditRepository auditRepo,
        AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators.SemanticCodeAnalyzer semanticAnalyzer)
    {
        _githubClient = githubClient ?? throw new ArgumentNullException(nameof(githubClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _agentRepo = agentRepo ?? throw new ArgumentNullException(nameof(agentRepo));
        _auditRepo = auditRepo ?? throw new ArgumentNullException(nameof(auditRepo));
        _semanticAnalyzer = semanticAnalyzer ?? throw new ArgumentNullException(nameof(semanticAnalyzer));
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

            // Persist the snapshot JSON to the audit trail for full traceability
            var snapshotJson = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            var auditEntry = new AgentOps.Application.UseCases.CreateAgentDefinition.AuditEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Action = "PersistPullRequestSnapshot",
                EntityType = "PullRequestSnapshot",
                EntityId = snapshot.Number.ToString(),
                Status = "SavedToAudit",
                Details = snapshotJson
            };
            var auditId = await _auditRepo.AppendAsync(auditEntry);
            Console.WriteLine($"🔒 Snapshot appended to audit log (AuditId: {auditId})");

            // Build a combined diff from available patches (fallback to file summaries)
            var combinedDiff = string.Join(Environment.NewLine, snapshot.Files.Where(f => !string.IsNullOrWhiteSpace(f.Patch)).Select(f => f.Patch));
            if (string.IsNullOrWhiteSpace(combinedDiff))
            {
                combinedDiff = string.Join(Environment.NewLine, snapshot.Files.Select(f => $"+ {f.Path}: {f.Status} (+{f.Additions} -{f.Deletions})"));
            }

            // Find the Code Reviewer agent
            var agents = await _agentRepo.ListAllAsync();
            var codeReviewer = agents.FirstOrDefault(a => a.Name == "Code Reviewer");
            
            // If no agent exists or ID is invalid, create a temporary one
            if (codeReviewer == null || string.IsNullOrEmpty(codeReviewer.Id.Value))
            {
                Console.WriteLine("⚠️  Creating temporary Code Reviewer agent for analysis...");
                // Create a minimal agent definition with all required parameters
                var tempAgentId = new AgentId("cli-code-reviewer-" + Guid.NewGuid().ToString());
                codeReviewer = new AgentDefinition(
                    tempAgentId,
                    "Code Reviewer (CLI)",
                    "Temporary agent for CLI PR analysis.",
                    "Analyze PR diffs for security issues",
                    new System.Collections.Generic.List<string> { "Detect secrets", "Detect dangerous APIs" },
                    new System.Collections.Generic.List<string> { "StaticCodeScan" },
                    new AgentConfiguration { RequiresAudit = true, AllowHallucination = false },
                    DateTime.UtcNow,
                    "1.0"
                );
                // Persist the temporary agent so it can be found by the evaluator
                await _agentRepo.AddAsync(codeReviewer);
                Console.WriteLine("✅ Temporary agent created and saved.");
            }

            // Evaluate the PR diff using existing analyzers (handler orchestrates secret/danger/dep analyzers)
            var req = new EvaluateAgentBehaviorRequest
            {
                AgentId = codeReviewer.Id.Value,
                ScenarioId = "code-review-security-suite-v1",
                OperatorId = "cli",
                Input = combinedDiff,
                Options = new EvaluationOptions { PersistArtifacts = true, AnonymizeEvidence = true },
                GitHubOwner = owner,
                GitHubRepo = repo,
                GitHubPRNumber = pr
            };

            var resp = await _evaluator.HandleAsync(req);

            // Call optional semantic analyzer (LLM-based, gracefully skipped if not configured)
            Console.WriteLine("\n🤖 Running semantic code analysis...");
            var semanticScenario = new AgentOps.Application.UseCases.EvaluateAgentBehavior.Models.EvaluationScenario
            {
                ScenarioId = "semantic-analysis-v1",
                Name = "Semantic Code Quality Analysis",
                Type = "CodeReview",
                TestVectors = new System.Collections.Generic.List<string> { combinedDiff }
            };
            var semanticResult = _semanticAnalyzer.Analyze(codeReviewer, semanticScenario);
            if (semanticResult.Findings.Any())
            {
                resp.TopFindings.AddRange(semanticResult.Findings.Take(3).Select(f => new AgentOps.Application.UseCases.EvaluateAgentBehavior.FindingSummary
                {
                    FindingId = f.FindingId,
                    Category = f.Category,
                    Severity = f.Severity,
                    Summary = f.Summary
                }));
            }

            // Display results
            DisplaySnapshot(snapshot);
            Console.WriteLine($"\n✅ PR analysis complete. Snapshot: {snapshot.SnapshotTimestamp:O}");
            Console.WriteLine($"Result: {resp.FinalStatus} - Risk: {resp.OverallRiskLevel}");
            Console.WriteLine($"Scores: Security {resp.Metrics.SecurityScore} / Compliance {resp.Metrics.ComplianceScore} / Consistency {resp.Metrics.ConsistencyScore}");
            if (resp.TopFindings != null && resp.TopFindings.Count > 0)
            {
                Console.WriteLine("Top findings:");
                foreach (var f in resp.TopFindings)
                {
                    Console.WriteLine($"- [{f.Severity}] {f.Summary} -> {f.Category}");
                }
            }
            if (!string.IsNullOrWhiteSpace(resp.ReportPath))
            {
                Console.WriteLine($"📁 Evaluation report saved at: {resp.ReportPath}");
            }
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
