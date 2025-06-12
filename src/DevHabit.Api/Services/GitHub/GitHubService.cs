using DevHabit.Api.Dtos.GitHub;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DevHabit.Api.Services.GitHub;

public sealed class GitHubService(
    IHttpClientFactory httpClientFactory,
    ILogger<RefitGitHubService> logger)
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy(),
        },
    };

    public async Task<GitHubUserProfileDto?> GetUserProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        HttpClient client = CreateGitHubClient(accessToken);

        HttpResponseMessage response = await client.GetAsync(new Uri("user", UriKind.Relative), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user profile. Status code: {StatusCode}", response.StatusCode);
            return null;
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonConvert.DeserializeObject<GitHubUserProfileDto>(content, JsonSettings);
    }

    public async Task<IReadOnlyList<GitHubEventDto>> GetUserEventsAsync(
        string username,
        string accessToken,
        int page,
        int perPage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        HttpClient client = CreateGitHubClient(accessToken);

        HttpResponseMessage response = await client.GetAsync(
            new Uri($"users/{username}/events?page={page}&per_page={perPage}", UriKind.Relative),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user events. Status code: {StatusCode}", response.StatusCode);
            return [];
        }

        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonConvert.DeserializeObject<List<GitHubEventDto>>(content, JsonSettings) ?? [];
    }

    private HttpClient CreateGitHubClient(string accessToken)
    {
        HttpClient client = httpClientFactory.CreateClient("github");
        client.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);

        return client;
    }
}
