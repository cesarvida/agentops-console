namespace AgentOps.Application.Dashboard
{
    /// <summary>
    /// CQRS query that requests the agent dashboard for a GitHub repository.
    /// </summary>
    public class GetDashboardQuery
    {
        /// <summary>GitHub repository owner (user or organisation).</summary>
        public string Owner { get; }

        /// <summary>GitHub repository name.</summary>
        public string Repo { get; }

        public GetDashboardQuery(string owner, string repo)
        {
            Owner = owner;
            Repo  = repo;
        }
    }
}
