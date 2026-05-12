using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AgentOps.GitHub
{
    /// <summary>
    /// GitHub HTTP client for API interactions.
    /// </summary>
    public class GitHubHttpClient
    {
        private readonly string _token;

        public GitHubHttpClient(string token)
        {
            _token = token ?? throw new ArgumentNullException(nameof(token));
        }

        /// <summary>
        /// Makes a POST request to a GitHub API endpoint.
        /// </summary>
        public async Task<string> PostAsync(string endpoint, string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(_token))
                throw new UnauthorizedAccessException("GITHUB_TOKEN not set.");

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint cannot be empty", nameof(endpoint));

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AgentOpsCLI/1.0");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            var url = $"https://api.github.com{endpoint}";
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Makes a GET request to a GitHub API endpoint.
        /// </summary>
        public async Task<string> GetAsync(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(_token))
                throw new UnauthorizedAccessException("GITHUB_TOKEN not set.");

            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint cannot be empty", nameof(endpoint));

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AgentOpsCLI/1.0");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            var url = $"https://api.github.com{endpoint}";
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }
    }
}
