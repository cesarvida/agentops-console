using System.Collections.Generic;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;

namespace AgentOps.Application.UseCases.EvaluateAgentBehavior.Evaluators
{
    public class AnalyzerResult
    {
        public List<Finding> Findings { get; set; } = new List<Finding>();
        public int Score { get; set; }
    }
}
