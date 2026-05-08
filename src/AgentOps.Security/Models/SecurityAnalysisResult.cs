using System.Collections.Generic;

namespace AgentOps.Security.Models
{
    public class SecurityAnalysisResult
    {
        public List<SecurityFinding> Findings { get; set; } = new List<SecurityFinding>();
        public int Score { get; set; } // 0..100
    }
}
