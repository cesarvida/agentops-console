using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AgentOps.CLI;
using AgentOps.CLI.Options;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Application.UseCases.ViewAuditTrail;
using AgentOps.Infrastructure.Persistence;
using AgentOps.Core.Governance;
using AgentOps.Core.Governance.Rules;
using AgentOps.Application.Governance;

// Check if running in CI/non-interactive mode for PR analysis
bool isCIPRAnalysis = args.Length >= 4 && args[0] == "analyze-pr";
bool isValidateAgent = args.Length >= 3 && args[0] == "validate-agent";

var host = Host.CreateDefaultBuilder(args)
	.ConfigureServices((context, services) =>
	{
		// Core governance services
		services.AddSingleton<IConsoleWriter, ConsoleWriter>();
		services.AddSingleton<DataPathsOptions>();
		services.AddSingleton<IClock, SystemClock>();
		
		// Agent repositories and handlers
		services.AddSingleton<IAgentDefinitionRepository>(sp =>
			new FileAgentDefinitionRepository(sp.GetRequiredService<DataPathsOptions>().AgentDefinitionsPath));
		services.AddSingleton<IAuditRepository>(sp =>
			new FileAuditRepository(sp.GetRequiredService<DataPathsOptions>().AuditPath));
		// Evaluation scenario repository (YAML)
		services.AddSingleton<AgentOps.Application.UseCases.EvaluateAgentBehavior.IEvaluationScenarioRepository, AgentOps.Infrastructure.Persistence.YamlEvaluationScenarioRepository>();
		// Evaluation report persistence
		services.AddSingleton<AgentOps.Application.UseCases.EvaluateAgentBehavior.IEvaluationReportRepository>(sp =>
			new AgentOps.Infrastructure.Persistence.FileEvaluationReportRepository(sp.GetRequiredService<DataPathsOptions>()));
		services.AddSingleton<AgentOps.Application.UseCases.EvaluateAgentBehavior.EvaluateAgentBehaviorHandler>(sp =>
			new AgentOps.Application.UseCases.EvaluateAgentBehavior.EvaluateAgentBehaviorHandler(
				sp.GetRequiredService<IAgentDefinitionRepository>(),
				sp.GetRequiredService<AgentOps.Application.UseCases.EvaluateAgentBehavior.IEvaluationScenarioRepository>(),
				sp.GetRequiredService<IAuditRepository>(),
				sp.GetRequiredService<AgentOps.Application.UseCases.EvaluateAgentBehavior.IEvaluationReportRepository>(),
				sp.GetRequiredService<AgentOps.Security.Interfaces.ISecurityAnalyzer>(),
				sp.GetRequiredService<AgentOps.Application.Interfaces.ICommentPoster>()));
		services.AddSingleton<AgentOps.CLI.EvaluateAgentBehaviorCommand>();
		services.AddSingleton<CreateAgentDefinitionHandler>();
		services.AddSingleton<CreateAgentDefinitionCommand>();
		services.AddSingleton<ListAgentsCommand>();
		// Audit trail reader and ViewAuditTrail command
		services.AddSingleton<IAuditTrailReader>(sp =>
			new AgentOps.Infrastructure.Persistence.FileAuditTrailReader(
				sp.GetRequiredService<DataPathsOptions>().AuditPath,
				sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<AgentOps.Infrastructure.Persistence.FileAuditTrailReader>>()));
		services.AddSingleton<ViewAuditTrailHandler>();
		services.AddSingleton<ViewAuditTrailCommand>();
		// Register governance rules and engine
		services.AddSingleton<IGovernanceRule, AllowedActionsRule>();
		services.AddSingleton<IGovernanceRule, ForbiddenActionsRule>();
		services.AddSingleton<IGovernanceRule, AuditLoggingRule>();
		services.AddSingleton<IGovernanceRule, OwnerDefinedRule>();
		services.AddSingleton<IGovernanceRule, VersionDefinedRule>();
		services.AddSingleton<GovernanceRuleEngine>();
		services.AddSingleton<ValidateAgentCommandHandler>();
		// Register security rules and analyzer for deterministic checks
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityRule, AgentOps.Security.Rules.PromptInjectionRule>();
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityRule, AgentOps.Security.Rules.ToolAbuseRule>();
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityRule, AgentOps.Security.Rules.SensitiveDataExposureRule>();
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityRule, AgentOps.Security.Rules.MissingSafetyRule>();
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityRule, AgentOps.Security.Rules.MissingRetentionPolicyRule>();
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityRule, AgentOps.Security.Rules.MissingLawfulBasisRule>();
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityRule, AgentOps.Security.Rules.UnclassifiedDataRule>();
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityRule, AgentOps.Security.Rules.MissingJustificationRule>();
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityRule, AgentOps.Security.Rules.NoComplianceRulesRule>();
		services.AddSingleton<AgentOps.Security.Interfaces.ISecurityAnalyzer, AgentOps.Security.SecurityAnalyzer>();
		services.AddSingleton<RunCodeReviewCommand>();
		services.AddSingleton<RunComplianceCheckCommand>();
		// GitHub PR Analyzer
		services.AddSingleton<AgentOps.GitHub.IGitHubPullRequestClient>(sp =>
			new AgentOps.GitHub.GitHubPullRequestClient(Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? ""));
		services.AddSingleton<AgentOps.GitHub.GitHubHttpClient>(sp =>
			new AgentOps.GitHub.GitHubHttpClient(Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? ""));
		services.AddSingleton<AgentOps.Application.Interfaces.ICommentPoster>(sp =>
			new AgentOps.Infrastructure.GitHub.GitHubCommentPoster(sp.GetRequiredService<AgentOps.GitHub.GitHubHttpClient>()));
		services.AddSingleton<AgentOps.CLI.Commands.AnalyzePullRequestCommand>();
		
		// Register optional LLM semantic analyzer (only if Azure OpenAI is configured)
		var azureOpenAIOptions = new AzureOpenAIOptions();
		if (azureOpenAIOptions.IsConfigured)
		{
			services.AddSingleton<AgentOps.Application.Interfaces.ILLMClient>(sp =>
				new AzureOpenAIClient(azureOpenAIOptions.Endpoint, azureOpenAIOptions.Key, 
					sp.GetRequiredService<ILogger<AzureOpenAIClient>>()));
			services.AddSingleton<AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators.SemanticCodeAnalyzer>(sp =>
				new AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators.SemanticCodeAnalyzer(
					sp.GetRequiredService<AgentOps.Application.Interfaces.ILLMClient>()));
		}
		else
		{
			// Semantic analyzer without LLM client (graceful degradation)
			services.AddSingleton<AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators.SemanticCodeAnalyzer>(sp =>
				new AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators.SemanticCodeAnalyzer(null));
		}
	})
	.ConfigureLogging(logging =>
	{
		logging.ClearProviders();
		logging.AddConsole();
	})
	.Build();

var console = host.Services.GetRequiredService<IConsoleWriter>();
var createAgentCmd = host.Services.GetRequiredService<CreateAgentDefinitionCommand>();
// Seed Code Reviewer agent if missing (persist via CreateAgentDefinitionHandler)
var agentRepoSvc = host.Services.GetRequiredService<IAgentDefinitionRepository>();
var createHandler = host.Services.GetRequiredService<CreateAgentDefinitionHandler>();
var existing = await agentRepoSvc.ListAllAsync();
if (!existing.Any(a => a.Name == "Code Reviewer"))
{
	var seedReq = new AgentOps.Application.UseCases.CreateAgentDefinition.CreateAgentDefinitionRequest
	{
		Name = "Code Reviewer",
		Description = "Deterministic code-review agent that detects secrets, dangerous APIs and dependency risks.",
		Purpose = "Review PR diffs for security, quality and compliance.",
		Rules = new System.Collections.Generic.List<string>
		{
			"No approve with vulnerabilities",
			"No hardcode secrets",
			"No suggest insecure code",
			"Explain every finding"
		},
		Tools = new System.Collections.Generic.List<string> { "StaticCodeScan", "DependencyCheck", "SecretDetection" },
		Configuration = new AgentOps.Core.Entities.AgentConfiguration { RequiresAudit = true, AllowHallucination = false }
	};
	var resp = await createHandler.HandleAsync(seedReq);
	if (resp.Status == "Success")
	{
		console.WriteLine("Seeded Code Reviewer agent.");
	}
}

// Seed Compliance Checker agent if missing
if (!existing.Any(a => a.Name == "Compliance Checker"))
{
	var seedReq = new AgentOps.Application.UseCases.CreateAgentDefinition.CreateAgentDefinitionRequest
	{
		Name = "Compliance Checker",
		Description = "Governed compliance checker for GDPR-like and internal policies.",
		Purpose = "Verificar cumplimiento normativo y políticas internas",
		Rules = new System.Collections.Generic.List<string>
		{
			"No permitir almacenamiento de PII sin base legal",
			"Exigir políticas de retención de datos",
			"Exigir trazabilidad y justificación",
			"Bloquear definiciones sin reglas de compliance explícitas"
		},
		Tools = new System.Collections.Generic.List<string> { "PolicyChecklist", "DataClassification", "RetentionValidation" },
		Configuration = new AgentOps.Core.Entities.AgentConfiguration { RequiresAudit = true, AllowHallucination = false }
	};
	var resp2 = await createHandler.HandleAsync(seedReq);
	if (resp2.Status == "Success")
	{
		console.WriteLine("Seeded Compliance Checker agent.");
	}
}

// Display governance console
console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
console.WriteLine("     AgentOps Governance Console [MVP]     ");
console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
console.WriteLine("");

// If running in CI mode for PR analysis, bypass interactive menu
if (isCIPRAnalysis && args.Length >= 4)
{
	var analyzePRCmd = host.Services.GetRequiredService<AgentOps.CLI.Commands.AnalyzePullRequestCommand>();
	try
	{
		if (int.TryParse(args[3], out int prNumber))
		{
			await analyzePRCmd.ExecuteAsync(args[1], args[2], prNumber);
		}
		else
		{
			console.WriteLine("❌ Invalid PR number provided");
		}
	}
	catch (Exception ex)
	{
		console.WriteLine($"❌ Error during PR analysis: {ex.Message}");
	}
}
else if (isValidateAgent && args.Length >= 2)
{
	var handler = host.Services.GetRequiredService<ValidateAgentCommandHandler>();
	try
	{
		var command = new ValidateAgentCommand(args[1]);
		var report = await handler.HandleAsync(command);
		DisplayGovernanceReport(report, console);
	}
	catch (Exception ex)
	{
		console.WriteLine($"❌ Error validating agent: {ex.Message}");
	}
}
else
{
	// Interactive menu mode
	console.WriteLine("1) Create New Agent Definition");
	console.WriteLine("2) List Agent Definitions");
	console.WriteLine("3) Exit");
	console.WriteLine("4) Evaluate Agent Behavior");
	console.WriteLine("5) View Audit Trail");
	console.WriteLine("6) Run Code Review (simulated)");
	console.WriteLine("7) Run Compliance Check (simulated)");
	console.WriteLine("8) Analyze GitHub PR (real PR from GitHub)");
	console.WriteLine("9) Validate Agent Governance (YAML)");
	console.WriteLine("");
	console.WriteLine("Select option: ");
	var opt = Console.ReadLine();

	if (opt == "1")
	{
		await createAgentCmd.ExecuteAsync();
	}
	else if (opt == "4")
	{
		var evalCmd = host.Services.GetRequiredService<EvaluateAgentBehaviorCommand>();
		await evalCmd.ExecuteAsync();
	}
	else if (opt == "2")
	{
		var listCmd = host.Services.GetRequiredService<ListAgentsCommand>();
		await listCmd.ExecuteAsync();
	}
	else if (opt == "5")
	{
		var auditCmd = host.Services.GetRequiredService<ViewAuditTrailCommand>();
		await auditCmd.ExecuteAsync();
	}
	else if (opt == "6")
	{
		var runCmd = host.Services.GetRequiredService<RunCodeReviewCommand>();
		await runCmd.ExecuteAsync();
	}
	else if (opt == "7")
	{
	    var runCmd = host.Services.GetRequiredService<RunComplianceCheckCommand>();
	    await runCmd.ExecuteAsync();
	}
	else if (opt == "8")
	{
		var analyzePRCmd = host.Services.GetRequiredService<AgentOps.CLI.Commands.AnalyzePullRequestCommand>();
		// Read owner, repo, and PR number from stdin
		var owner = Console.ReadLine() ?? "";
		var repo = Console.ReadLine() ?? "";
		var prNumberStr = Console.ReadLine() ?? "";
		if (int.TryParse(prNumberStr, out var prNumber))
		{
			await analyzePRCmd.ExecuteAsync(owner, repo, prNumber);
		}
		else
		{
			await analyzePRCmd.ExecuteAsync();
		}
	}
	else if (opt == "9")
	{
		console.WriteLine("Enter path to agent YAML file: ");
		var yamlPath = Console.ReadLine() ?? "";
		if (!string.IsNullOrWhiteSpace(yamlPath))
		{
			try
			{
				var handler = host.Services.GetRequiredService<ValidateAgentCommandHandler>();
				var command = new ValidateAgentCommand(yamlPath);
				var report = await handler.HandleAsync(command);
				
				// Display report in console
				DisplayGovernanceReport(report, console);
			}
			catch (Exception ex)
			{
				console.WriteLine($"❌ Error validating agent: {ex.Message}");
			}
		}
	}
	else
	{
		console.WriteLine("Exiting AgentOps Console. Goodbye.");
	}
}

// Ensure required data directory exists
var paths = host.Services.GetRequiredService<DataPathsOptions>();
System.IO.Directory.CreateDirectory(paths.AgentDefinitionsPath);
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(paths.AuditPath) ?? ".");
System.IO.Directory.CreateDirectory(paths.EvaluationsPath);

await host.StopAsync();

// Display governance report in console
void DisplayGovernanceReport(AgentOps.Core.Governance.GovernanceReport report, IConsoleWriter console)
{
	string statusEmoji = report.FinalStatus switch
	{
		"APPROVED" => "✅",
		"REVIEW" => "⚠️",
		"BLOCKED" => "❌",
		_ => "❓"
	};

	console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
	console.WriteLine("   Governance Validation Report");
	console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
	console.WriteLine($"Agent: {report.AgentName} v{report.AgentVersion}");
	console.WriteLine($"Status: {report.FinalStatus} {statusEmoji}");
	console.WriteLine($"Governance Score: {report.GovernanceScore}/100");
	console.WriteLine($"Critical Violations: {report.CriticalViolations}");
	console.WriteLine($"Warnings: {report.WarningViolations}");
	console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

	if (report.RuleResults.Any(r => !r.IsCompliant))
	{
		console.WriteLine("Violations:");
		foreach (var rule in report.RuleResults.Where(r => !r.IsCompliant))
		{
			string icon = rule.Severity switch
			{
				AgentOps.Core.Governance.RuleSeverity.Critical => "🔴",
				AgentOps.Core.Governance.RuleSeverity.Warning => "🟡",
				_ => "ℹ️"
			};
			console.WriteLine($"\n{icon} {rule.RuleName} ({rule.Severity})");
			foreach (var violation in rule.Violations)
			{
				console.WriteLine($"  - {violation}");
			}
		}
	}
	else
	{
		console.WriteLine("✅ Agent passed all governance rules!");
	}

	console.WriteLine("");
}

// SystemClock implementation
public class SystemClock : IClock
{
	public DateTime UtcNow => DateTime.UtcNow;
}
