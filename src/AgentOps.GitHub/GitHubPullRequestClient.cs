using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
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

        public async Task<PullRequestSnapshot> GetPullRequestAsync(string owner, string repository, int number)
        {
            if (string.IsNullOrWhiteSpace(_token))
                throw new UnauthorizedAccessException("GITHUB_TOKEN not set.");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AgentOpsCLI/1.0");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            var prUrl = $"https://api.github.com/repos/{owner}/{repository}/pulls/{number}";
            var prResp = await client.GetAsync(prUrl);
            prResp.EnsureSuccessStatusCode();
            var prJson = await prResp.Content.ReadAsStringAsync();
            using var prDoc = JsonDocument.Parse(prJson);

            var root = prDoc.RootElement;
            var title = root.GetProperty("title").GetString() ?? $"PR #{number}";
            var body = root.GetProperty("body").GetString() ?? string.Empty;
            var user = root.GetProperty("user").GetProperty("login").GetString() ?? string.Empty;
            var state = root.GetProperty("state").GetString() ?? string.Empty;
            var baseRef = root.GetProperty("base").GetProperty("ref").GetString() ?? string.Empty;
            var headRef = root.GetProperty("head").GetProperty("ref").GetString() ?? string.Empty;
            var commits = root.GetProperty("commits").GetInt32();

            // Fetch files (may be paginated; this simple implementation gets the first page)
            var filesUrl = $"https://api.github.com/repos/{owner}/{repository}/pulls/{number}/files";
            var filesResp = await client.GetAsync(filesUrl);
            filesResp.EnsureSuccessStatusCode();
            var filesJson = await filesResp.Content.ReadAsStringAsync();
            using var filesDoc = JsonDocument.Parse(filesJson);

            var files = new List<DiffFile>();
            foreach (var el in filesDoc.RootElement.EnumerateArray())
            {
                var path = el.GetProperty("filename").GetString() ?? string.Empty;
                var status = el.GetProperty("status").GetString() ?? string.Empty;
                var additions = el.GetProperty("additions").GetInt32();
                var deletions = el.GetProperty("deletions").GetInt32();
                var changes = el.GetProperty("changes").GetInt32();
                var patch = el.TryGetProperty("patch", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                var previous = el.TryGetProperty("previous_filename", out var prev) ? prev.GetString() : null;
                var isBinary = string.IsNullOrEmpty(patch) && el.TryGetProperty("blob_url", out _);

                files.Add(new DiffFile
                {
                    Path = path,
                    Status = status,
                    Additions = additions,
                    Deletions = deletions,
                    Changes = changes,
                    Patch = patch ?? string.Empty,
                    PreviousPath = previous,
                    IsBinary = isBinary
                });
            }

            var snapshot = new PullRequestSnapshot
            {
                Owner = owner,
                Repository = repository,
                Number = number,
                Title = title,
                Description = body,
                Author = user,
                State = state,
                BaseBranch = baseRef,
                HeadBranch = headRef,
                CommitCount = commits,
                Files = files,
                SnapshotTimestamp = DateTime.UtcNow,
                Url = prUrl
            };

            return snapshot;
        }
    }
}
