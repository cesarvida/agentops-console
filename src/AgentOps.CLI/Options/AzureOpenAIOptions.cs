using System;

namespace AgentOps.CLI.Options
{
    /// <summary>
    /// Configuration for Azure OpenAI semantic governance analysis.
    /// Reads from standard Azure OpenAI environment variables first,
    /// then falls back to the legacy AGENTOPS_ prefixed names.
    /// When endpoint and key are both absent, semantic analysis is disabled.
    /// </summary>
    public class AzureOpenAIOptions
    {
        public string? Endpoint       { get; set; }
        public string? Key            { get; set; }
        public string  DeploymentName { get; set; } = "gpt-5.4-nano";

        public AzureOpenAIOptions()
        {
            // Standard Azure OpenAI env vars (preferred)
            Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                    ?? Environment.GetEnvironmentVariable("AGENTOPS_OPENAI_ENDPOINT");

            Key = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
               ?? Environment.GetEnvironmentVariable("AGENTOPS_OPENAI_KEY");

            var deployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME");
            if (!string.IsNullOrWhiteSpace(deployment))
                DeploymentName = deployment;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Endpoint) &&
            !string.IsNullOrWhiteSpace(Key);
    }
}
