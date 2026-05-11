using System.Threading.Tasks;
using AgentOps.GitHub.Models;

namespace AgentOps.GitHub
{
    public interface IGitHubPullRequestClient
    {
        Task<PullRequestSnapshot> GetPullRequestAsync(string owner, string repository, int number);
    }
}
