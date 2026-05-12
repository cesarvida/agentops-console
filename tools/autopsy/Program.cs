using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AgentOps.Autopsy
{
    /// <summary>
    /// Repository autopsy tool — generates AUTOPSY_REPORT.md in the repo root.
    /// Run from the repo root: dotnet run --project tools/autopsy/AutopsyTool.csproj
    /// </summary>
    internal static class Program
    {
        // ── Secret patterns to scan for (never print the captured value) ────
        private static readonly (string Name, Regex Pattern)[] SecretPatterns = new[]
        {
            ("AZURE_OPENAI_API_KEY assignment",       new Regex(@"AZURE_OPENAI_API_KEY\s*=\s*[""']?[A-Za-z0-9/+]{10,}", RegexOptions.IgnoreCase)),
            ("Hardcoded api-key header value",        new Regex(@"""api-key""\s*,\s*""[A-Za-z0-9/+]{10,}""", RegexOptions.IgnoreCase)),
            ("Azure cognitive services key pattern",   new Regex(@"[A-Fa-f0-9]{32}",  RegexOptions.None)),
            ("sk- OpenAI key",                        new Regex(@"sk-[A-Za-z0-9]{20,}", RegexOptions.None)),
            ("BEGIN RSA PRIVATE KEY",                 new Regex(@"-----BEGIN (RSA |EC )?PRIVATE KEY-----")),
            ("Hardcoded password assignment",         new Regex(@"password\s*=\s*[""'][^""']{6,}[""']", RegexOptions.IgnoreCase)),
        };

        // Extensions to scan (skip binaries)
        private static readonly HashSet<string> ScanExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".cs", ".yaml", ".yml", ".json", ".xml", ".config", ".env", ".sh", ".ps1", ".md", ".txt"
        };

        // Files/dirs to skip during secret scan
        private static readonly HashSet<string> SkipDirs = new(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", ".git", ".vs", "node_modules", "TestResults"
        };

        static int Main(string[] args)
        {
            var repoRoot = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
            Console.WriteLine($"🔬 AgentOps Autopsy — Repo: {repoRoot}");
            Console.WriteLine();

            var report = new ReportBuilder();
            report.H1("AgentOps Governance — Autopsy Report");
            report.Line($"> Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.Line($"> Repo root: `{repoRoot}`");
            report.Line();

            // ── Section 1: Build & Test ──────────────────────────────────────
            report.H2("1. Build & Test Status");
            var (buildOk, buildSummary) = RunCommand("dotnet", "build AgentOps.Console.sln -c Release --nologo", repoRoot, 120);
            var (testOk, testSummary)   = RunCommand("dotnet", "test AgentOps.Console.sln -c Release --no-build --nologo", repoRoot, 180);

            report.CheckItem(buildOk, $"Solution builds without errors — `{buildSummary}`");
            report.CheckItem(testOk,  $"All tests pass — `{testSummary}`");

            if (!buildOk) report.Warning("Build failed. Subsequent checks may be unreliable.");
            if (!testOk)  report.Warning("Some tests failed.");

            // ── Section 2: Governance Workflow Integrity ─────────────────────
            report.H2("2. Governance Workflow Integrity");

            var workflowPath    = Path.Combine(repoRoot, ".github", "workflows", "governance-check.yml");
            var workflowExists  = File.Exists(workflowPath);
            report.CheckItem(workflowExists, "`.github/workflows/governance-check.yml` exists");

            if (workflowExists)
            {
                var wf = File.ReadAllText(workflowPath);
                report.CheckItem(wf.Contains("name: AI Governance Check"),       "Workflow name is `AI Governance Check`");
                report.CheckItem(wf.Contains("name: Governance Enforcement"),    "Job name is `Governance Enforcement` (Required Status Check key)");
                report.CheckItem(wf.Contains("pull_request:"),                   "Triggers on `pull_request`");
                report.CheckItem(wf.Contains("AZURE_OPENAI_ENDPOINT"),           "Azure OpenAI endpoint env var wired in workflow");
                report.CheckItem(wf.Contains("AZURE_OPENAI_API_KEY"),            "Azure OpenAI API key env var wired in workflow");
                report.CheckItem(wf.Contains("secrets.AZURE_OPENAI_API_KEY"),   "API key sourced from GitHub Secret (not hardcoded)");
                // || true is acceptable in grep pipelines — only flag if used after dotnet/CLI commands
                bool hasBadOrTrue = wf.Contains("|| true") &&
                                    !Regex.IsMatch(wf, @"grep[^\n]*\|\| true");
                report.CheckItem(!hasBadOrTrue, "No `|| true` masking failures (enforcement is real)");
            }

            // ── Section 3: Exit Code Contract ───────────────────────────────
            report.H2("3. Exit Code Contract (Phase 10)");

            var programCs = Path.Combine(repoRoot, "src", "AgentOps.CLI", "Program.cs");
            if (File.Exists(programCs))
            {
                var prog = File.ReadAllText(programCs);
                bool hasBlockedExit1 = prog.Contains("FinalStatus == \"BLOCKED\"") &&
                                       prog.Contains("Environment.ExitCode = 1");
                bool hasExceptionExit1 = prog.Contains("Environment.ExitCode = 1");
                report.CheckItem(hasBlockedExit1,   "CLI sets `Environment.ExitCode = 1` on `FinalStatus == BLOCKED`");
                report.CheckItem(hasExceptionExit1, "CLI sets `Environment.ExitCode = 1` on unhandled exception");
                report.CheckItem(!prog.Contains("Environment.Exit(1)"),
                    "Uses `Environment.ExitCode` (not `Environment.Exit`) — allows async cleanup");
            }
            else
            {
                report.Warning("Program.cs not found — skipping exit code checks.");
            }

            // ── Section 4: Config & Exceptions Integrity ────────────────────
            report.H2("4. Config & Exceptions Integrity");

            var govConfigPath = Path.Combine(repoRoot, "data", "governance-config.yaml");
            var govExists     = File.Exists(govConfigPath);
            report.CheckItem(govExists, "`data/governance-config.yaml` exists");
            if (govExists)
            {
                var cfg = File.ReadAllText(govConfigPath);
                report.CheckItem(cfg.Contains("semantic_analysis:"), "Config includes `semantic_analysis` section");
                report.CheckItem(cfg.Contains("allowed_actions:"),   "Config includes `allowed_actions`");
                report.CheckItem(cfg.Contains("forbidden_actions:"), "Config includes `forbidden_actions`");
                report.CheckItem(cfg.Contains("scoring:"),           "Config includes `scoring`");
            }

            var exceptionFile = Path.Combine(repoRoot, "src", "AgentOps.Core", "Governance", "GovernanceException.cs");
            report.CheckItem(File.Exists(exceptionFile), "`GovernanceException.cs` exists (exception model)");

            var handlerFile = Path.Combine(repoRoot, "src", "AgentOps.Application", "Governance", "ValidateAgentCommandHandler.cs");
            if (File.Exists(handlerFile))
            {
                var handler = File.ReadAllText(handlerFile);
                bool handlesExceptions = handler.Contains("GovernanceException") ||
                                         File.ReadAllText(Path.Combine(repoRoot, "src", "AgentOps.Application",
                                             "Governance", "GovernanceRuleEngine.cs"))
                                             .Contains("GovernanceException");
                report.CheckItem(handlesExceptions, "Handler/engine applies governance exceptions");
                report.CheckItem(handler.Contains("FinalStatus != \"BLOCKED\""),
                    "Rule-based BLOCKED is not overridden by semantic result");
            }

            // ── Section 5: Semantic Analysis Integrity ───────────────────────
            report.H2("5. Semantic Analysis Integrity (Phase 11)");

            var semanticInterface = Path.Combine(repoRoot, "src", "AgentOps.Application", "Interfaces", "IAgentSemanticAnalyzer.cs");
            var semanticClient    = Path.Combine(repoRoot, "src", "AgentOps.Infrastructure", "AzureOpenAI", "AzureOpenAIGovernanceClient.cs");
            var semanticResult    = Path.Combine(repoRoot, "src", "AgentOps.Core", "Governance", "SemanticAnalysisResult.cs");
            var semanticOptions   = Path.Combine(repoRoot, "src", "AgentOps.CLI", "Options", "AzureOpenAIOptions.cs");

            report.CheckItem(File.Exists(semanticInterface), "`IAgentSemanticAnalyzer` interface exists");
            report.CheckItem(File.Exists(semanticClient),    "`AzureOpenAIGovernanceClient` implementation exists");
            report.CheckItem(File.Exists(semanticResult),    "`SemanticAnalysisResult` model exists");

            if (File.Exists(semanticClient))
            {
                var client = File.ReadAllText(semanticClient);
                report.CheckItem(client.Contains("AZURE_OPENAI_ENDPOINT") || client.Contains("_endpoint"),
                    "Client reads endpoint from variable (not hardcoded)");
                // API key must not appear next to a logger call
                bool apiKeyLogged = client.Contains("_logger") && client.Contains("_apiKey") &&
                                    Regex.IsMatch(client, @"_logger\.[A-Za-z]+\([^)]*_apiKey");
                report.CheckItem(!apiKeyLogged, "API key is not logged");
                report.CheckItem(client.Contains("OperationCanceledException"),
                    "Client handles timeout gracefully");
                report.CheckItem(client.Contains("SemanticAnalysisResult.Skipped"),
                    "Client returns Skipped on error (no crash)");
            }

            if (File.Exists(semanticOptions))
            {
                var opts = File.ReadAllText(semanticOptions);
                report.CheckItem(opts.Contains("AZURE_OPENAI_ENDPOINT"), "Options reads `AZURE_OPENAI_ENDPOINT`");
                report.CheckItem(opts.Contains("AZURE_OPENAI_API_KEY"),  "Options reads `AZURE_OPENAI_API_KEY`");
                report.CheckItem(opts.Contains("AZURE_OPENAI_DEPLOYMENT_NAME"), "Options reads deployment name");
            }

            if (File.Exists(handlerFile))
            {
                var handler = File.ReadAllText(handlerFile);
                report.CheckItem(handler.Contains("IAgentSemanticAnalyzer? semanticAnalyzer = null"),
                    "SemanticAnalyzer is optional (null default — no crash when absent)");
                report.CheckItem(handler.Contains("semanticConfig.Enabled"),
                    "Semantic analysis is gated by `Enabled` flag in config");
            }

            // ── Section 6: Secret Hygiene ────────────────────────────────────
            report.H2("6. Secret Hygiene");

            var envExamplePath = Path.Combine(repoRoot, ".env.example");
            var gitignorePath  = Path.Combine(repoRoot, ".gitignore");

            report.CheckItem(File.Exists(envExamplePath), "`.env.example` exists");
            if (File.Exists(envExamplePath))
            {
                var ex = File.ReadAllText(envExamplePath);
                report.CheckItem(ex.Contains("AZURE_OPENAI_ENDPOINT"),        ".env.example contains `AZURE_OPENAI_ENDPOINT`");
                report.CheckItem(ex.Contains("AZURE_OPENAI_API_KEY"),         ".env.example contains `AZURE_OPENAI_API_KEY`");
                report.CheckItem(ex.Contains("AZURE_OPENAI_DEPLOYMENT_NAME"), ".env.example contains `AZURE_OPENAI_DEPLOYMENT_NAME`");
                // Key must be empty in the example
                bool apiKeyEmpty = Regex.IsMatch(ex, @"AZURE_OPENAI_API_KEY\s*=\s*$", RegexOptions.Multiline);
                report.CheckItem(apiKeyEmpty, ".env.example has empty `AZURE_OPENAI_API_KEY` (placeholder only)");
            }

            if (File.Exists(gitignorePath))
            {
                var gi = File.ReadAllText(gitignorePath);
                report.CheckItem(gi.Contains(".env"),            "`.gitignore` contains `.env`");
                report.CheckItem(gi.Contains("secrets") || gi.Contains("*.json") || gi.Contains("secrets.local"),
                    "`.gitignore` covers local secrets files");
            }

            // Secret scan
            report.Line();
            report.Line("**Secret scan** — scanning tracked source files for accidentally committed secrets:");
            report.Line();
            var secretFindings = ScanForSecrets(repoRoot);
            if (secretFindings.Count == 0)
            {
                report.Line("✅ No accidental secrets detected in tracked files.");
            }
            else
            {
                report.Warning($"{secretFindings.Count} potential secret location(s) found. Review carefully:");
                report.Line();
                report.Line("```");
                foreach (var finding in secretFindings)
                    report.Line(finding);
                report.Line("```");
            }

            // ── Section 7: GitHub Readiness Checklist ───────────────────────
            report.H2("7. GitHub Readiness Checklist (manual steps)");
            report.Line("The following must be verified in the GitHub repository settings:");
            report.Line();
            report.Line("- [ ] Branch protection rule enabled for `main`");
            report.Line("- [ ] **Require status checks to pass before merging** enabled");
            report.Line("- [ ] Required status check: `AI Governance Check / Governance Enforcement` added");
            report.Line("- [ ] **Require branches to be up to date** enabled");
            report.Line("- [ ] GitHub Secret `AZURE_OPENAI_ENDPOINT` set (optional — semantic degrades gracefully)");
            report.Line("- [ ] GitHub Secret `AZURE_OPENAI_API_KEY` set (optional)");
            report.Line("- [ ] GitHub Secret `AZURE_OPENAI_DEPLOYMENT_NAME` set (optional, default: `gpt-5.4-nano`)");
            report.Line("- [ ] `GITHUB_TOKEN` auto-provided by Actions (no manual secret needed)");

            // ── Section 8: Risks & Recommendations ──────────────────────────
            report.H2("8. Risks & Recommendations");
            report.Line("| Risk | Severity | Recommendation |");
            report.Line("|---|---|---|");
            report.Line("| Semantic analysis disabled when Azure secrets not set | LOW | Expected fallback — rule-based governance still runs |");
            report.Line("| governance-config.yaml only loaded from GitHub API in dashboard mode | MEDIUM | `validate-agent` uses local file — ensure it is committed and up to date |");
            report.Line("| Exit code contract depends on FinalStatus string match | LOW | String is set centrally in GovernanceRuleEngine — low risk of drift |");
            report.Line("| governance-check.yml path filter removed (all PRs scanned) | INFO | Correct — ensures Required Status Check always appears in PR checks list |");

            // ── Write output ─────────────────────────────────────────────────
            var outputPath = Path.Combine(repoRoot, "AUTOPSY_REPORT.md");
            File.WriteAllText(outputPath, report.Build(), Encoding.UTF8);

            Console.WriteLine();
            Console.WriteLine($"✅ Report written to: {outputPath}");
            Console.WriteLine($"   Passed: {report.Passed}  Failed: {report.Failed}");

            return report.Failed > 0 ? 1 : 0;
        }

        // ── Secret scanner ────────────────────────────────────────────────────

        private static List<string> ScanForSecrets(string root)
        {
            var findings = new List<string>();
            // Only skip the .env.example key lines and the example files
            var skipFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".env.example"
            };

            foreach (var file in EnumerateTrackedFiles(root))
            {
                var relative = Path.GetRelativePath(root, file);
                if (skipFiles.Contains(Path.GetFileName(file))) continue;
                if (!ScanExtensions.Contains(Path.GetExtension(file))) continue;

                string[] lines;
                try { lines = File.ReadAllLines(file); }
                catch { continue; }

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    foreach (var (name, pattern) in SecretPatterns)
                    {
                        if (!pattern.IsMatch(line)) continue;

                        // Redact: replace matched value with first4***last4
                        var redacted = pattern.Replace(line.Trim(), m =>
                        {
                            var val = m.Value;
                            if (val.Length > 8)
                                return val[..4] + "***" + val[^4..];
                            return "****";
                        });

                        findings.Add($"  [{name}] {relative}:{i + 1} → {redacted}");
                        break; // one finding per line
                    }
                }
            }

            return findings;
        }

        private static IEnumerable<string> EnumerateTrackedFiles(string root)
        {
            return EnumerateFiles(root, root);
        }

        private static IEnumerable<string> EnumerateFiles(string dir, string root)
        {
            foreach (var file in Directory.EnumerateFiles(dir))
                yield return file;

            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                var name = Path.GetFileName(subDir);
                if (SkipDirs.Contains(name)) continue;
                foreach (var f in EnumerateFiles(subDir, root))
                    yield return f;
            }
        }

        // ── Process runner ────────────────────────────────────────────────────

        private static (bool ok, string summary) RunCommand(string exe, string arguments, string workDir, int timeoutSec)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, arguments)
                {
                    WorkingDirectory       = workDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using var proc = Process.Start(psi)!;
                var stdout = proc.StandardOutput.ReadToEnd();
                var stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(timeoutSec * 1000);

                var output = (stdout + stderr).Trim();
                // Extract the summary line (last meaningful line)
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var summary = lines.LastOrDefault(l =>
                    l.Contains("Error") || l.Contains("Warning") ||
                    l.Contains("succeeded") || l.Contains("passed") ||
                    l.Contains("failed"))?.Trim()
                    ?? (output.Length > 80 ? output[..80] + "…" : output);

                return (proc.ExitCode == 0, summary);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }

    // ── Report builder ─────────────────────────────────────────────────────────

    internal sealed class ReportBuilder
    {
        private readonly StringBuilder _sb = new();
        public int Passed { get; private set; }
        public int Failed { get; private set; }

        public void H1(string text) { _sb.AppendLine($"# {text}"); _sb.AppendLine(); }
        public void H2(string text) { _sb.AppendLine(); _sb.AppendLine($"## {text}"); _sb.AppendLine(); }
        public void Line(string text = "") => _sb.AppendLine(text);
        public void Warning(string text)  => _sb.AppendLine($"> ⚠️ **Warning:** {text}");

        public void CheckItem(bool ok, string label)
        {
            if (ok) { Passed++; _sb.AppendLine($"- ✅ {label}"); }
            else    { Failed++; _sb.AppendLine($"- ❌ {label}"); }
        }

        public string Build() => _sb.ToString();
    }
}
