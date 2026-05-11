using System;

namespace AgentOps.CLI.Options
{
    /// <summary>
    /// Configuration for Azure OpenAI semantic analysis.
    /// If endpoint and key are not set, semantic analysis is disabled.
    /// </summary>
    public class AzureOpenAIOptions
    {
        public string? Endpoint { get; set; }
        public string? Key { get; set; }
        public string DeploymentName { get; set; } = "gpt-4";

        public AzureOpenAIOptions()
        {
            Endpoint = Environment.GetEnvironmentVariable("AGENTOPS_OPENAI_ENDPOINT");
            Key = Environment.GetEnvironmentVariable("AGENTOPS_OPENAI_KEY");
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(Key);
    }
}
