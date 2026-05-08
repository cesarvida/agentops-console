using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AgentOps.CLI;
using AgentOps.Application.UseCases.CreateAgentDefinition;
using AgentOps.Application.UseCases.ViewAuditTrail;
using AgentOps.Infrastructure.Persistence;

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
		services.AddSingleton<AgentOps.Application.UseCases.EvaluateAgentBehavior.EvaluateAgentBehaviorHandler>();
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
        services.AddSingleton<RunCodeReviewCommand>();
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

// Display governance console
console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
console.WriteLine("     AgentOps Governance Console [MVP]     ");
console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
console.WriteLine("");
console.WriteLine("1) Create New Agent Definition");
console.WriteLine("2) List Agent Definitions");
console.WriteLine("3) Exit");
console.WriteLine("4) Evaluate Agent Behavior");
console.WriteLine("5) View Audit Trail");
console.WriteLine("6) Run Code Review (simulated)");
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
else
{
	console.WriteLine("Exiting AgentOps Console. Goodbye.");
}

// Ensure required data directory exists
var paths = host.Services.GetRequiredService<DataPathsOptions>();
System.IO.Directory.CreateDirectory(paths.AgentDefinitionsPath);
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(paths.AuditPath) ?? ".");
System.IO.Directory.CreateDirectory(paths.EvaluationsPath);

await host.StopAsync();

// SystemClock implementation
public class SystemClock : IClock
{
	public DateTime UtcNow => DateTime.UtcNow;
}
