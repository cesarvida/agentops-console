using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentOps.GitHub.Models;

namespace AgentOps.GitHub
{
    public class GitHubPullRequestClient : IGitHubPullRequestClient
    {
        private readonly string _token;

        public GitHubPullRequestClient(string token)
        {
            _token = token ?? string.Empty;
        }

        public Task<PullRequestSnapshot> GetPullRequestAsync(string owner, string repository, int number)
        {
            if (string.IsNullOrWhiteSpace(_token))
                throw new UnauthorizedAccessException("GITHUB_TOKEN not set.");

            // Minimal stub implementation: return an empty snapshot.
            var snapshot = new PullRequestSnapshot
            {
                Owner = owner,
                Repository = repository,
                Number = number,
                Title = $"PR #{number}",
                Author = "unknown",
                State = "open",
                BaseBranch = "main",
                HeadBranch = "feature",
                CommitCount = 1,
                SnapshotTimestamp = DateTime.UtcNow,
                Url = $"https://github.com/{owner}/{repository}/pull/{number}",
                Files = new List<DiffFile>()
            };

            return Task.FromResult(snapshot);
        }
    }
}
