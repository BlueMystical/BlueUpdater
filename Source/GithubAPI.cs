using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;

/// <summary>GitHub Repository Version Checker</summary>
public static class GitHubVersionChecker
{
    private static readonly HttpClient client = new HttpClient();

    static GitHubVersionChecker()
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GitHub API client)");
    }

    /// <summary>Retrieves the Version of the Latest Release of a GitHub Repository.</summary>
    /// <param name="repositoryOwner">Name of the Repo Owner</param>
    /// <param name="repositoryName">Name of the Repository</param>
    public static async Task<string> GetLatestReleaseVersion(string repositoryOwner, string repositoryName)
    {
        string url = $"https://api.github.com/repos/{repositoryOwner}/{repositoryName}/releases/latest";
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        JObject json = JObject.Parse(responseBody);
        return json["tag_name"].ToString();
    }
}
