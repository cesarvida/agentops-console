using System;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOps.Application.Rendering;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;
using Microsoft.Extensions.Logging;

namespace AgentOps.CLI.Commands;

/// <summary>
/// Renders an EvaluationReport as a GitHub PR comment in Markdown format.
/// Usage: agentops render-pr-comment --report <path-to-report.json>
/// Output: Writes Markdown to agentops-comment.md or stdout
/// </summary>
public sealed class RenderPRCommentCommand
{
    private readonly ILogger<RenderPRCommentCommand> _logger;

    public RenderPRCommentCommand(ILogger<RenderPRCommentCommand> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes the render-pr-comment command.
    /// </summary>
    /// <param name="reportPath">Path to EvaluationReport JSON file</param>
    /// <param name="outputPath">Optional path to write Markdown comment (default: stdout)</param>
    public async Task<int> ExecuteAsync(string reportPath, string? outputPath = null)
    {
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                Console.Error.WriteLine("❌ Error: --report path is required");
                return 1;
            }

            if (!File.Exists(reportPath))
            {
                Console.Error.WriteLine($"❌ Error: Report file not found: {reportPath}");
                return 1;
            }

            _logger.LogInformation("Reading EvaluationReport from: {ReportPath}", reportPath);

            // Load report
            var json = await File.ReadAllTextAsync(reportPath);
            var report = JsonSerializer.Deserialize<EvaluationReport>(json);

            if (report == null)
            {
                Console.Error.WriteLine("❌ Error: Failed to deserialize EvaluationReport");
                return 1;
            }

            // Render markdown comment
            var markdown = PRCommentRenderer.RenderComment(report);

            // Write output
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                // Write to stdout
                Console.WriteLine(markdown);
                _logger.LogInformation("Comment rendered to stdout");
            }
            else
            {
                // Write to file
                await File.WriteAllTextAsync(outputPath, markdown);
                _logger.LogInformation("Comment rendered to: {OutputPath}", outputPath);
                Console.WriteLine($"✅ Comment saved to: {outputPath}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render PR comment");
            Console.Error.WriteLine($"❌ Error: {ex.Message}");
            return 1;
        }
    }
}
