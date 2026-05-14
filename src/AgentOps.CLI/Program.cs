using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AgentOps.CLI;
using AgentOps.CLI.Commands;
using AgentOps.Application.Analysis;
using AgentOps.Application.Analysis.Pipeline;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Analysis;
using AgentOps.Core.Analysis.Detectors;

// ── Argument parsing ──────────────────────────────────────────────────────────
bool isCIMode     = args.Length >= 4 && args[0] == "analyze-pr";
bool isDirectFile = args.Length >= 2 && args[0] == "analyze";

int    prNumberArg = 0;
string prOwnerArg  = string.Empty;
string prRepoArg   = string.Empty;
string? outputDir  = null;
bool   postComment = args.Contains("--post-comment");

for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--pr"     && int.TryParse(args[i + 1], out int pn)) prNumberArg = pn;
    if (args[i] == "--owner")  prOwnerArg = args[i + 1];
    if (args[i] == "--repo")   prRepoArg  = args[i + 1];
    if (args[i] == "--output") outputDir  = args[i + 1];
}

// ── DI Container ─────────────────────────────────────────────────────────────
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IConsoleWriter, ConsoleWriter>();

        // Pipeline infrastructure
        services.AddSingleton<SafeContentExtractor>();
        services.AddSingleton<ContentNormalizer>();
        services.AddSingleton<ContextClassifier>();
        services.AddSingleton<PromptSanitizer>();

        // Register all 6 detection rules
        services.AddSingleton<IPromptDetector, PromptInjectionRule>();
        services.AddSingleton<IPromptDetector, ToolAbuseRule>();
        services.AddSingleton<IPromptDetector, DataExfiltrationRule>();
        services.AddSingleton<IPromptDetector, HiddenInstructionRule>();
        services.AddSingleton<IPromptDetector, ObfuscationRule>();
        services.AddSingleton<IPromptDetector, PythonPromptStringRule>();

        // Register analyzer
        services.AddSingleton<PromptAnalyzer>();

        // Register CLI commands
        services.AddSingleton<AnalyzeFileCommand>();

        // GitHub PR support
        services.AddSingleton<AgentOps.GitHub.IGitHubPullRequestClient>(sp =>
            new AgentOps.GitHub.GitHubPullRequestClient(
                Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? ""));
        services.AddSingleton<AgentOps.GitHub.GitHubHttpClient>(sp =>
            new AgentOps.GitHub.GitHubHttpClient(
                Environment.GetEnvironmentVariable("GITHUB_TOKEN") ?? ""));
        services.AddSingleton<ICommentPoster>(sp =>
            new AgentOps.Infrastructure.GitHub.GitHubCommentPoster(
                sp.GetRequiredService<AgentOps.GitHub.GitHubHttpClient>()));
        services.AddSingleton<AnalyzePullRequestCommand>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Warning);
    })
    .Build();

// ── Banner ────────────────────────────────────────────────────────────────────
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("       AgentOps — Prompt Security Analyzer      ");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.ResetColor();
Console.WriteLine();

// ── Mode: CI PR Analysis ──────────────────────────────────────────────────────
if (isCIMode)
{
    var prCmd = host.Services.GetRequiredService<AnalyzePullRequestCommand>();
    if (int.TryParse(args[3], out int prNum))
    {
        var code = await prCmd.ExecuteAsync(args[1], args[2], prNum);
        Environment.ExitCode = code;
    }
    else
    {
        Console.WriteLine("❌ Invalid PR number");
        Environment.ExitCode = 1;
    }
    return;
}

// ── Mode: Direct file analysis ────────────────────────────────────────────────
if (isDirectFile)
{
    var filePath = args[1];
    var fileCmd = host.Services.GetRequiredService<AnalyzeFileCommand>();
    var code = await fileCmd.ExecuteAsync(filePath, postComment, prNumberArg, prOwnerArg, prRepoArg, outputDir);
    Environment.ExitCode = code;
    return;
}

// ── Mode: Interactive Menu ────────────────────────────────────────────────────
var analyzeCmd = host.Services.GetRequiredService<AnalyzeFileCommand>();

while (true)
{
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║       AgentOps — Prompt Analyzer         ║");
    Console.WriteLine("╠══════════════════════════════════════════╣");
    Console.WriteLine("║  1) 🔍 Analizar un archivo               ║");
    Console.WriteLine("║  2) 📊 Ver último reporte                ║");
    Console.WriteLine("║  3) 🚪 Salir                             ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");
    Console.Write("\nElige una opción (1-3): ");

    var choice = Console.ReadLine()?.Trim();

    if (choice == "3" || choice == "q" || choice == "exit")
        break;

    if (choice == "1")
    {
        Console.WriteLine();
        Console.WriteLine("¿Ruta del archivo a analizar?");
        Console.WriteLine("(Markdown .md o Python .py)");
        Console.Write("→ ");
        var filePath = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("❌ Ruta vacía.\n");
            continue;
        }

        await analyzeCmd.ExecuteAsync(filePath, outputDir: "outputs");

        Console.WriteLine("\n¿Qué quieres hacer?");
        Console.WriteLine("  1) Analizar otro archivo");
        Console.WriteLine("  2) Volver al menú");
        Console.Write("→ ");
        var next = Console.ReadLine()?.Trim();
        if (next != "1")
            continue;
    }
    else if (choice == "2")
    {
        var reports = Directory.GetFiles("outputs", "analysis-*.json", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f)
            .ToArray();

        if (reports.Length == 0)
        {
            Console.WriteLine("\n📭 No hay reportes guardados aún.\n");
        }
        else
        {
            Console.WriteLine($"\n📄 Último reporte: {reports[0]}\n");
            var content = await File.ReadAllTextAsync(reports[0]);
            Console.WriteLine(content[..Math.Min(500, content.Length)]);
            Console.WriteLine();
        }
    }
    else
    {
        Console.WriteLine("❌ Opción no válida.\n");
    }
}

Console.WriteLine("\n👋 Hasta luego!");
