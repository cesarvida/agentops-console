using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Governance;
using Microsoft.Extensions.Logging;

namespace AgentOps.Infrastructure.AzureOpenAI
{
    /// <summary>
    /// Semantic governance analyzer that calls Azure OpenAI to assess an agent YAML definition.
    /// Implements <see cref="IAgentSemanticAnalyzer"/>. Never throws — always returns a valid result.
    /// </summary>
    public class AzureOpenAIGovernanceClient : IAgentSemanticAnalyzer
    {
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly string _deploymentName;
        private readonly ILogger<AzureOpenAIGovernanceClient> _logger;

        private const string ApiVersion   = "2024-02-15-preview";
        private const string SystemPrompt =
            "You are an AI governance auditor. You analyze AI agent definitions and detect governance risks, " +
            "ambiguity, missing constraints, unsafe behavior, weak boundaries, excessive permissions, " +
            "unclear tool usage, and risky operational patterns.";

        public AzureOpenAIGovernanceClient(
            string endpoint,
            string apiKey,
            string deploymentName,
            ILogger<AzureOpenAIGovernanceClient> logger)
        {
            _endpoint       = (endpoint       ?? throw new ArgumentNullException(nameof(endpoint))).TrimEnd('/');
            _apiKey         = apiKey          ?? throw new ArgumentNullException(nameof(apiKey));
            _deploymentName = !string.IsNullOrWhiteSpace(deploymentName) ? deploymentName : "gpt-5.4-nano";
            _logger         = logger          ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<SemanticAnalysisResult> AnalyzeAgentSemanticsAsync(
            string agentYaml,
            SemanticAnalysisConfig config,
            CancellationToken cancellationToken = default)
        {
            if (!config.Enabled)
                return SemanticAnalysisResult.Skipped("Semantic analysis is disabled in governance config.");

            var timeoutSeconds = config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 5;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                var url = $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions" +
                          $"?api-version={ApiVersion}";

                var requestBody = new
                {
                    temperature = 0.2,
                    max_tokens  = config.MaxTokens > 0 ? config.MaxTokens : 800,
                    messages = new[]
                    {
                        new { role = "system", content = SystemPrompt },
                        new { role = "user",   content = BuildUserPrompt(agentYaml) }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);

                using var httpClient = new HttpClient();
                // API key header — never logged
                httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                var response    = await httpClient.PostAsync(url, httpContent, cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("⚠️ Azure OpenAI returned {Status}.", response.StatusCode);
                    return SemanticAnalysisResult.Skipped($"API error: {response.StatusCode}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                return ParseResponse(responseJson);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("⚠️ Semantic analysis timed out after {Seconds}s.", timeoutSeconds);
                return SemanticAnalysisResult.Skipped($"Timeout after {timeoutSeconds}s");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Semantic analysis failed: {Message}", ex.Message);
                return SemanticAnalysisResult.Skipped($"Error: {ex.Message}");
            }
        }

        // ── Response parsing ──────────────────────────────────────────────────

        private static SemanticAnalysisResult ParseResponse(string responseJson)
        {
            try
            {
                using var doc    = JsonDocument.Parse(responseJson);
                var choices      = doc.RootElement.GetProperty("choices");

                if (choices.GetArrayLength() == 0)
                    return SemanticAnalysisResult.Skipped("Semantic analysis unavailable: empty model response");

                var content = choices[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                // Parse the JSON object the model was instructed to return
                using var inner = JsonDocument.Parse(content);
                var root        = inner.RootElement;

                var riskLevel = root.TryGetProperty("risk_level", out var rl)
                    ? (rl.GetString() ?? "LOW").ToUpperInvariant()
                    : "LOW";

                var issues = new List<string>();
                if (root.TryGetProperty("issues", out var issuesArr))
                    foreach (var item in issuesArr.EnumerateArray())
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrEmpty(s)) issues.Add(s);
                    }

                var recommendations = new List<string>();
                if (root.TryGetProperty("recommendations", out var recsArr))
                    foreach (var item in recsArr.EnumerateArray())
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrEmpty(s)) recommendations.Add(s);
                    }

                return new SemanticAnalysisResult
                {
                    RiskLevel       = riskLevel,
                    Issues          = issues,
                    Recommendations = recommendations,
                    IsAvailable     = true
                };
            }
            catch
            {
                return new SemanticAnalysisResult
                {
                    IsAvailable     = false,
                    RiskLevel       = "LOW",
                    Issues          = new List<string> { "Semantic analysis unavailable: invalid model response" },
                    Recommendations = new List<string>(),
                    ErrorMessage    = "Invalid JSON response from model"
                };
            }
        }

        // ── Prompt builder ────────────────────────────────────────────────────

        private static string BuildUserPrompt(string agentYaml) =>
            "Analyze the following agent YAML.\n\n" +
            "Return ONLY valid JSON.\n" +
            "Do not include markdown.\n" +
            "Do not include explanations outside JSON.\n\n" +
            "Required JSON format:\n" +
            "{\n" +
            "  \"risk_level\": \"LOW\" | \"MEDIUM\" | \"HIGH\",\n" +
            "  \"issues\": [\"...\"],\n" +
            "  \"recommendations\": [\"...\"]\n" +
            "}\n\n" +
            "Rules:\n" +
            "- Be strict.\n" +
            "- If the agent has vague instructions, missing limits, unsafe permissions, unclear environment " +
            "scope, or weak operational constraints, raise the risk.\n" +
            "- If uncertain, choose the higher risk level.\n" +
            "- HIGH means the PR should be blocked.\n" +
            "- MEDIUM means the PR should require review.\n" +
            "- LOW means acceptable.\n\n" +
            $"Agent YAML:\n{agentYaml}";
    }
}
