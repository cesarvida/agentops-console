using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AgentOps.Application.Interfaces;
using AgentOps.Core.Governance;

namespace AgentOps.Demo
{
    /// <summary>
    /// Deterministic fake semantic analyzer for demo and test purposes.
    /// Returns a pre-set risk level regardless of the YAML content.
    /// Never calls Azure — safe to use without credentials.
    /// </summary>
    public sealed class FakeAgentSemanticAnalyzer : IAgentSemanticAnalyzer
    {
        private readonly string _riskLevel;

        /// <param name="riskLevel">One of: LOW, MEDIUM, HIGH</param>
        public FakeAgentSemanticAnalyzer(string riskLevel = "LOW")
        {
            _riskLevel = riskLevel.ToUpperInvariant();
        }

        public Task<SemanticAnalysisResult> AnalyzeAgentSemanticsAsync(
            string agentYaml,
            SemanticAnalysisConfig config,
            CancellationToken cancellationToken = default)
        {
            var issues = _riskLevel switch
            {
                "HIGH"   => new List<string> { "Agent has overly broad permissions", "Missing operational constraints" },
                "MEDIUM" => new List<string> { "Rate limiting policy is ambiguous" },
                _        => new List<string>()
            };

            var recommendations = _riskLevel switch
            {
                "HIGH"   => new List<string> { "Restrict actions to minimum required", "Add explicit environment scope" },
                "MEDIUM" => new List<string> { "Define explicit rate limit policy" },
                _        => new List<string> { "Agent definition looks acceptable" }
            };

            return Task.FromResult(new SemanticAnalysisResult
            {
                RiskLevel       = _riskLevel,
                Issues          = issues,
                Recommendations = recommendations,
                IsAvailable     = true,
                ErrorMessage    = null
            });
        }
    }
}
