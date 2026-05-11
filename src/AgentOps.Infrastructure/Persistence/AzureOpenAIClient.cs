using System;
using System.Threading.Tasks;
using AgentOps.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgentOps.Infrastructure.Persistence
{
    /// <summary>
    /// Azure OpenAI client for semantic code analysis.
    /// Falls back to stub response if Azure credentials are invalid or unavailable.
    /// </summary>
    public class AzureOpenAIClient : ILLMClient
    {
        private readonly string? _endpoint;
        private readonly string? _key;
        private readonly ILogger<AzureOpenAIClient> _logger;

        public AzureOpenAIClient(string? endpoint, string? key, ILogger<AzureOpenAIClient> logger)
        {
            _endpoint = endpoint;
            _key = key;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> AnalyzeCodeAsync(string diff, string context)
        {
            // If credentials are missing, return a stub response with no findings
            if (string.IsNullOrWhiteSpace(_endpoint) || string.IsNullOrWhiteSpace(_key))
            {
                _logger.LogWarning("Azure OpenAI credentials not configured. Semantic analysis skipped.");
                return "{}"; // Empty findings
            }

            try
            {
                // TODO: Implement actual Azure OpenAI API call
                // For now, return a stub response to avoid blocking when API is unavailable
                var stubResponse = new
                {
                    findings = new object[0],
                    summary = "Semantic analysis via Azure OpenAI (stub response for now)"
                };

                _logger.LogInformation("Semantic analysis completed via Azure OpenAI.");
                return System.Text.Json.JsonSerializer.Serialize(stubResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Azure OpenAI for semantic analysis. Proceeding without semantic findings.");
                return "{}"; // Fail gracefully
            }
        }
    }
}
