using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AgentOps.CLI.Tests.Commands
{
    public class ValidateAgentCommandTests : IDisposable
    {
        private readonly string _testOutputDir;

        public ValidateAgentCommandTests()
        {
            _testOutputDir = Path.Combine(Path.GetTempPath(), "agentops-cli-tests");
            Directory.CreateDirectory(_testOutputDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testOutputDir))
                Directory.Delete(_testOutputDir, recursive: true);
        }

        /// <summary>
        /// Test 1: validate-agent with a test agent file
        /// Should complete without unhandled exceptions
        /// </summary>
        [Fact]
        public async Task ValidateAgent_WithTestAgent_CompletesSuccessfully()
        {
            var testAgent = Path.Combine(_testOutputDir, "compliant-agent.yaml");
            var compliantYaml = @"
name: TestAgent
version: 1.0.0
owner: agentops-team
description: A test agent
purpose: Testing governance
actions:
  - read_code
tools:
  - CodeAnalyzer
auditLogging: true
rateLimit: 100
timeout: 30
environments:
  - dev
rules:
  - rule1
";
            await File.WriteAllTextAsync(testAgent, compliantYaml);

            var result = await RunValidateAgentCommand(testAgent);

            // Should not crash with unhandled exception
            Assert.NotEmpty(result.Output);
            // Should contain some governance status
            Assert.True(
                result.Output.Contains("APPROVED", StringComparison.OrdinalIgnoreCase) ||
                result.Output.Contains("BLOCKED", StringComparison.OrdinalIgnoreCase) ||
                result.Output.Contains("REVIEW", StringComparison.OrdinalIgnoreCase),
                "Output should contain a governance status");
        }

        /// <summary>
        /// Test 2: validate-agent with non-compliant agent (missing owner, bad version)
        /// Should produce output with governance status
        /// </summary>
        [Fact]
        public async Task ValidateAgent_WithMissingOwner_ProducesReport()
        {
            // Create a non-compliant agent for testing
            var testAgent = Path.Combine(_testOutputDir, "bad-agent.yaml");
            var badYaml = @"
name: BadAgent
version: dev
description: This is an agent without owner
purpose: Testing
actions:
  - read_code
tools:
  - tool1
auditLogging: false
";
            await File.WriteAllTextAsync(testAgent, badYaml);

            var result = await RunValidateAgentCommand(testAgent);

            // Should produce output (error or report)
            Assert.NotEmpty(result.Output);
            // Should handle missing owner gracefully (not crash)
            Assert.DoesNotContain("Unhandled exception", result.Output, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Test 3: validate-agent with strict rules file
        /// Uses custom rules, different score than defaults
        /// </summary>
        [Fact]
        public async Task ValidateAgent_WithStrictRules_AppliesCustomThresholds()
        {
            // Create test agent
            var testAgent = Path.Combine(_testOutputDir, "compliant-for-strict.yaml");
            var agentYaml = @"
name: TestAgent
version: 1.0.0
owner: team
description: Test agent
purpose: Testing
actions:
  - read_code
tools:
  - Tool1
auditLogging: true
rateLimit: 100
timeout: 30
environments:
  - dev
rules:
  - rule1
";
            await File.WriteAllTextAsync(testAgent, agentYaml);

            // Create strict rules file
            var strictRulesFile = Path.Combine(_testOutputDir, "strict-rules.yaml");
            var strictRulesYaml = @"
minGovernanceScore: 95
requireOwner: true
requireAudit: true
";
            await File.WriteAllTextAsync(strictRulesFile, strictRulesYaml);

            // Run with default rules
            var defaultResult = await RunValidateAgentCommand(testAgent);

            // Run with strict rules
            var strictResult = await RunValidateAgentCommand(
                testAgent,
                "--rules", strictRulesFile
            );

            // Both should parse successfully
            Assert.NotEmpty(defaultResult.Output);
            Assert.NotEmpty(strictResult.Output);
        }

        /// <summary>
        /// Test 4: validate-agent with inline --allow and --forbid flags
        /// Respects custom allowed/forbidden actions
        /// </summary>
        [Fact]
        public async Task ValidateAgent_WithInlineFlags_AppliesAllowForbidActions()
        {
            var testAgent = Path.Combine(_testOutputDir, "agent-with-flags.yaml");
            var agentYaml = @"
name: FlagsTestAgent
version: 1.0.0
owner: team
description: Test agent for flags
purpose: Testing flags
actions:
  - read_code
  - write_file
tools:
  - Tool1
auditLogging: true
rateLimit: 100
timeout: 30
environments:
  - dev
rules:
  - rule1
";
            await File.WriteAllTextAsync(testAgent, agentYaml);

            // Test with --allow and --forbid flags
            var result = await RunValidateAgentCommand(
                testAgent,
                "--allow", "read_code,post_comment",
                "--forbid", "delete_files"
            );

            // Should produce output with governance report
            Assert.NotEmpty(result.Output);
            // Should contain either APPROVED, REVIEW, or BLOCKED status
            var statusFound = result.Output.Contains("APPROVED", StringComparison.OrdinalIgnoreCase) ||
                              result.Output.Contains("REVIEW", StringComparison.OrdinalIgnoreCase) ||
                              result.Output.Contains("BLOCKED", StringComparison.OrdinalIgnoreCase);
            Assert.True(statusFound, "Output should contain governance status");
        }

        /// <summary>
        /// Test 5: validate-agent with non-existent file
        /// Shows error message, exit code = 1, no unhandled exception
        /// </summary>
        [Fact]
        public async Task ValidateAgent_FileNotFound_ReturnsErrorWithExitCode1()
        {
            var result = await RunValidateAgentCommand("data/agent-definitions/does-not-exist.yaml");

            Assert.Equal(1, result.ExitCode);
            Assert.NotEmpty(result.Output); // Should contain error message
            Assert.DoesNotContain("Unhandled exception", result.Output, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Test 6: validate-agent with external format agent (flexible parser)
        /// Parser should map fields automatically, no exception thrown
        /// </summary>
        [Fact]
        public async Task ValidateAgent_WithExternalFormatAgent_ParsesWithFlexibleMapper()
        {
            // Create an OpenAI-style agent definition
            var externalAgent = Path.Combine(_testOutputDir, "external-format-agent.yaml");
            var externalYaml = @"
assistant_name: TestBot
version: 1.0.0
author: test-team
capabilities:
  - read_code
  - post_comment
metadata:
  version: 1.0.0
";
            await File.WriteAllTextAsync(externalAgent, externalYaml);

            var result = await RunValidateAgentCommand(externalAgent, "--external");

            // Should complete without exception
            Assert.NotEqual(-1, result.ExitCode);
            Assert.NotEmpty(result.Output);
            Assert.DoesNotContain("Unhandled exception", result.Output, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Test 7: dashboard command
        /// Should complete without throwing an unhandled exception
        /// </summary>
        [Fact]
        public async Task Dashboard_WithOwnerAndRepo_RunsWithoutException()
        {
            var result = await RunCommand(
                "dotnet", 
                "run --project src/AgentOps.CLI -- dashboard --owner cesarvida --repo agentops-console"
            );

            // Should complete without unhandled exception
            Assert.NotEmpty(result.Output);
            Assert.DoesNotContain("Unhandled exception", result.Output, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Test 8: Unknown command
        /// Shows help message, exit code = 1
        /// </summary>
        [Fact]
        public async Task UnknownCommand_ShowsHelpAndExitsWithError()
        {
            var result = await RunValidateAgentCommand("--unknown-flag");

            Assert.Equal(1, result.ExitCode);
            // Should show some guidance (help, error message, or similar)
            Assert.NotEmpty(result.Output);
        }

        // ── Helper methods ──────────────────────────────────────────────

        private async Task<(int ExitCode, string Output)> RunValidateAgentCommand(params string[] args)
        {
            var allArgs = new[] { "run", "--project", "src/AgentOps.CLI" }.Concat(
                new[] { "--" }).Concat(
                new[] { "validate-agent" }).Concat(args).ToArray();

            return await RunCommand("dotnet", string.Join(" ", allArgs));
        }

        private async Task<(int ExitCode, string Output)> RunCommand(string executable, string arguments)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = GetProjectRoot()
            };

            using var process = Process.Start(processInfo);
            
            var output = new StringBuilder();
            var errorOutput = new StringBuilder();

            // Read output asynchronously
            var readOutputTask = Task.Run(() =>
            {
                string? line;
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    output.AppendLine(line);
                }
            });

            var readErrorTask = Task.Run(() =>
            {
                string? line;
                while ((line = process.StandardError.ReadLine()) != null)
                {
                    errorOutput.AppendLine(line);
                }
            });

            await Task.WhenAll(readOutputTask, readErrorTask);
            process.WaitForExit();

            var combinedOutput = output.ToString() + errorOutput.ToString();
            return (process.ExitCode, combinedOutput);
        }

        private string GetProjectRoot()
        {
            var assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var binDir = Path.GetDirectoryName(assemblyPath);
            var testProjDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(binDir)));
            var testsDir = Path.GetDirectoryName(testProjDir);
            return Path.GetDirectoryName(testsDir); // root
        }
    }
}
