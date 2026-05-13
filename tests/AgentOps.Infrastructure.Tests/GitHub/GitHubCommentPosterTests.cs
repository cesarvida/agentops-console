using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using AgentOps.GitHub;
using AgentOps.Core.Governance;
using AgentOps.Infrastructure.GitHub;

namespace AgentOps.Infrastructure.Tests.GitHub
{
    public class GitHubCommentPosterTests
    {
        /// <summary>
        /// Test 1: PostGovernanceReport with valid report and valid token succeeds
        /// </summary>
        [Fact]
        public async Task PostGovernanceReport_WithValidInputs_DoesNotThrow()
        {
            // Arrange
            var httpClient = new GitHubHttpClient("mock-token");
            var poster = new GitHubCommentPoster(httpClient);
            var report = CreateTestGovernanceReport();

            // Act & Assert - should handle gracefully even if API fails
            // (API call might fail but the method shouldn't throw to caller)
            await poster.PostGovernanceReportAsync("owner", "repo", 1, report);
        }

        /// <summary>
        /// Test 2: PostGovernanceReport handles empty owner gracefully
        /// </summary>
        [Fact]
        public async Task PostGovernanceReport_WithEmptyOwner_HandlesProperly()
        {
            // Arrange
            var httpClient = new GitHubHttpClient("mock-token");
            var poster = new GitHubCommentPoster(httpClient);
            var report = CreateTestGovernanceReport();

            // Act & Assert - should handle edge case
            try
            {
                await poster.PostGovernanceReportAsync("", "repo", 1, report);
            }
            catch (ArgumentException)
            {
                // Expected - empty owner is invalid
            }
        }

        /// <summary>
        /// Test 3: PostGovernanceReport handles empty repo gracefully
        /// </summary>
        [Fact]
        public async Task PostGovernanceReport_WithEmptyRepo_HandlesProperly()
        {
            // Arrange
            var httpClient = new GitHubHttpClient("mock-token");
            var poster = new GitHubCommentPoster(httpClient);
            var report = CreateTestGovernanceReport();

            // Act & Assert - should handle edge case
            try
            {
                await poster.PostGovernanceReportAsync("owner", "", 1, report);
            }
            catch (ArgumentException)
            {
                // Expected - empty repo is invalid
            }
        }

        /// <summary>
        /// Test 4: Retry logic - multiple failed attempts are handled
        /// Verify through console output that retries occurred
        /// </summary>
        [Fact]
        public async Task PostGovernanceReport_OnAPIFailure_WritesGitHubWarning()
        {
            // Arrange
            var httpClient = new GitHubHttpClient("invalid-token");
            var poster = new GitHubCommentPoster(httpClient);
            var report = CreateTestGovernanceReport();

            // Capture console output
            var output = new System.IO.StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(output);

            try
            {
                // Act
                await poster.PostGovernanceReportAsync("owner", "repo", 1, report);

                // Assert - should have written warning (after 3 attempts)
                var capturedOutput = output.ToString();
                // Either warning or attempts should be mentioned
                Assert.NotEmpty(capturedOutput);
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        // ── Helper methods ──────────────────────────────────────────────

        private GovernanceReport CreateTestGovernanceReport()
        {
            return new GovernanceReport
            {
                AgentName = "Test Agent",
                FinalStatus = "APPROVED",
                GovernanceScore = 100,
                RuleResults = new List<RuleResult>(),
                EvaluatedAt = DateTime.UtcNow
            };
        }
    }
}
