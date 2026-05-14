using AgentOps.Application.Analysis;
using AgentOps.Core.Analysis.Pipeline;

namespace AgentOps.CLI.Commands;

public class AnalyzeFileCommand
{
    private readonly PromptAnalyzer _analyzer;

    public AnalyzeFileCommand(PromptAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public async Task<int> ExecuteAsync(string filePath, bool postComment = false,
        int prNumber = 0, string owner = "", string repo = "", string? outputDir = null)
    {
        if (!File.Exists(filePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ File not found: {filePath}");
            Console.ResetColor();
            return 1;
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nAnalyzing: {Path.GetFileName(filePath)}");
        Console.WriteLine(new string('─', 50));
        Console.ResetColor();

        var report = await _analyzer.AnalyzeAsync(filePath);

        PrintReport(report);

        if (outputDir != null)
        {
            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir,
                $"analysis-{Path.GetFileNameWithoutExtension(filePath)}-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
            await File.WriteAllTextAsync(outputPath, report.ToAuditJson());
            Console.WriteLine($"\n📄 Report saved: {outputPath}");
        }

        return report.Decision == "BLOCK" ? 1 : 0;
    }

    private static void PrintReport(PromptFileSafetyReport report)
    {
        var (decisionEmoji, decisionColor) = report.Decision switch
        {
            "PASS"   => ("✅", ConsoleColor.Green),
            "REVIEW" => ("⚠️ ", ConsoleColor.Yellow),
            _        => ("🚫", ConsoleColor.Red)
        };

        Console.WriteLine();
        Console.WriteLine("╔" + new string('═', 58) + "╗");
        Console.WriteLine("║" + "              ANÁLISIS DE SEGURIDAD                      " + "║");
        Console.WriteLine("╠" + new string('═', 58) + "╣");
        Console.WriteLine($"║  Archivo    : {report.FileName,-44}║");
        Console.WriteLine($"║  Tipo       : {report.FileType,-44}║");
        Console.WriteLine($"║  Amenazas   : {report.Findings.Count + " detectadas",-44}║");
        if (report.ObfuscationDetected)
            Console.WriteLine($"║  Ofuscación : {"⚠️  DETECTADA",-44}║");
        if (report.HiddenContentDetected)
            Console.WriteLine($"║  Oculto     : {"⚠️  DETECTADO",-44}║");
        Console.WriteLine("╠" + new string('═', 58) + "╣");

        if (report.Findings.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("║  Sin amenazas detectadas. El archivo parece seguro.     ║");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("║  AMENAZAS DETECTADAS:                                    ║");
            Console.WriteLine("║                                                          ║");

            foreach (var f in report.Findings.OrderByDescending(x => x.Severity))
            {
                var (sev, color) = f.Severity switch
                {
                    "CRITICAL" => ("🔴 CRITICAL", ConsoleColor.Red),
                    "HIGH"     => ("🟠 HIGH    ", ConsoleColor.DarkYellow),
                    "MEDIUM"   => ("🟡 MEDIUM  ", ConsoleColor.Yellow),
                    _          => ("🔵 LOW     ", ConsoleColor.Blue)
                };

                Console.ForegroundColor = color;
                var threatLine = $"  {sev}  {f.RuleId,-12} conf:{f.ConfidenceScore:P0}  línea {f.LineNumber}";
                Console.WriteLine($"║  {threatLine,-56}║");
                Console.ResetColor();

                var evidenceTrimmed = f.Evidence.Length > 54 ? f.Evidence[..51] + "..." : f.Evidence;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"║  \"{evidenceTrimmed,-54}\"║");
                Console.ResetColor();
                Console.WriteLine($"║  → {f.Recommendation[..Math.Min(52, f.Recommendation.Length)],-54}║");
                Console.WriteLine("║                                                          ║");
            }
        }

        Console.WriteLine("╠" + new string('═', 58) + "╣");
        Console.Write($"║  RISK SCORE : ");
        Console.ForegroundColor = decisionColor;
        Console.Write($"{report.RiskScore}/100");
        Console.ResetColor();
        Console.WriteLine(new string(' ', 42) + "║");

        Console.Write($"║  DECISIÓN   : ");
        Console.ForegroundColor = decisionColor;
        Console.Write($"{decisionEmoji} {report.Decision}");
        Console.ResetColor();
        Console.WriteLine(new string(' ', 42) + "║");

        Console.Write($"║  RAZÓN      : ");
        var reason = report.DecisionReason.Length > 43 ? report.DecisionReason[..40] + "..." : report.DecisionReason;
        Console.WriteLine($"{reason,-43}║");
        Console.WriteLine("╚" + new string('═', 58) + "╝");
        Console.WriteLine();
    }
}
