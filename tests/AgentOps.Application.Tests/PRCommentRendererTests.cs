using Xunit;
using AgentOps.Application.Rendering;
using AgentOps.Application.UseCases.EvaluateAgentBehavior.Models;

namespace AgentOps.Application.Tests;

public class PRCommentRendererTests
{
    [Fact]
    public void RenderComment_WithPASSReport_IncludesGreenCheckmark()
    {
        // Arrange
        var report = new EvaluationReport
        {
            EvaluationId = "eval-123",
            FinalStatus = "PASS",
            OverallRiskLevel = "Low",
            Metrics = new Metrics
            {
                SecurityScore = 95,
                ComplianceScore = 90,
                ConsistencyScore = 92,
                ExplainabilityScore = 88,
                CombinedQualityScore = 91,
                FindingsCount = 0,
                CriticalFindingsCount = 0
            },
            Findings = new List<Finding>(),
            Recommendations = new List<Recommendation>()
        };

        // Act
        var comment = PRCommentRenderer.RenderComment(report);

        // Assert
        Assert.Contains("✅ AgentOps PR Analysis — PASS", comment);
        Assert.Contains("agentops-pr-analysis", comment);
        Assert.Contains("| Security |", comment);
        Assert.Contains("91/100", comment);
    }

    [Fact]
    public void RenderComment_WithREVIEWReport_IncludesWarningEmoji()
    {
        // Arrange
        var report = new EvaluationReport
        {
            EvaluationId = "eval-456",
            FinalStatus = "REVIEW",
            OverallRiskLevel = "Medium",
            Metrics = new Metrics
            {
                SecurityScore = 70,
                ComplianceScore = 65,
                ConsistencyScore = 72,
                ExplainabilityScore = 60,
                CombinedQualityScore = 67,
                FindingsCount = 3,
                CriticalFindingsCount = 0
            },
            Findings = new List<Finding>
            {
                new Finding
                {
                    FindingId = "find-1",
                    Category = "Policy Bypass",
                    Severity = "High",
                    Summary = "Potential instruction override detected",
                    Location = "line 45",
                    Confidence = 0.85
                },
                new Finding
                {
                    FindingId = "find-2",
                    Category = "Data Exfiltration",
                    Severity = "Medium",
                    Summary = "External endpoint in configuration",
                    Location = "line 78",
                    Confidence = 0.70
                }
            },
            Recommendations = new List<Recommendation>()
        };

        // Act
        var comment = PRCommentRenderer.RenderComment(report);

        // Assert
        Assert.Contains("⚠️ AgentOps PR Analysis — REVIEW", comment);
        Assert.Contains("Findings (Top 5)", comment);
        Assert.Contains("High", comment);
        Assert.Contains("Potential instruction override", comment);
    }

    [Fact]
    public void RenderComment_WithBLOCKReport_IncludesRedXemoji()
    {
        // Arrange
        var report = new EvaluationReport
        {
            EvaluationId = "eval-789",
            FinalStatus = "BLOCK",
            OverallRiskLevel = "Critical",
            Metrics = new Metrics
            {
                SecurityScore = 10,
                ComplianceScore = 5,
                ConsistencyScore = 15,
                ExplainabilityScore = 8,
                CombinedQualityScore = 9,
                FindingsCount = 5,
                CriticalFindingsCount = 2
            },
            Findings = new List<Finding>
            {
                new Finding
                {
                    FindingId = "critical-1",
                    Category = "Prompt Injection",
                    Severity = "Critical",
                    Summary = "Direct instruction override attempt",
                    Confidence = 0.95
                },
                new Finding
                {
                    FindingId = "critical-2",
                    Category = "Tool Abuse",
                    Severity = "Critical",
                    Summary = "Privilege escalation code detected",
                    Confidence = 0.92
                }
            },
            Recommendations = new List<Recommendation>
            {
                new Recommendation
                {
                    RecommendationId = "rec-1",
                    Title = "Reject this PR immediately",
                    Description = "This PR contains critical security violations",
                    SeverityImpact = "Critical",
                    EffortEstimate = "None - Auto-reject"
                }
            }
        };

        // Act
        var comment = PRCommentRenderer.RenderComment(report);

        // Assert
        Assert.Contains("❌ AgentOps PR Analysis — BLOCK", comment);
        Assert.Contains("Critical Findings | 2", comment);
        Assert.Contains("Direct instruction override", comment);
        Assert.Contains("Reject this PR immediately", comment);
    }

    [Fact]
    public void RenderComment_WithMoreThanFiveFindings_ShowsEllipsis()
    {
        // Arrange
        var findings = new List<Finding>();
        for (int i = 0; i < 8; i++)
        {
            findings.Add(new Finding
            {
                FindingId = $"find-{i}",
                Category = "Test",
                Severity = "Medium",
                Summary = $"Finding {i}",
                Confidence = 0.50
            });
        }

        var report = new EvaluationReport
        {
            EvaluationId = "eval-multi",
            FinalStatus = "REVIEW",
            OverallRiskLevel = "Medium",
            Metrics = new Metrics { CombinedQualityScore = 60, FindingsCount = 8 },
            Findings = findings,
            Recommendations = new List<Recommendation>()
        };

        // Act
        var comment = PRCommentRenderer.RenderComment(report);

        // Assert
        Assert.Contains("... and 3 more findings", comment);
    }

    [Fact]
    public void RenderComment_ThrowsOnNullReport()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => PRCommentRenderer.RenderComment(null!));
    }

    [Fact]
    public void GetCommentMarker_ReturnsConsistentMarker()
    {
        // Act
        var marker = PRCommentRenderer.GetCommentMarker();

        // Assert
        Assert.Equal("<!-- agentops-pr-analysis -->", marker);
    }
}
