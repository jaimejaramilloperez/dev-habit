using DevHabit.Api.Dtos.GitHub;
using Refit;

namespace DevHabit.Api.Services.GitHub;

public sealed class RefitGitHubService(
    IGitHubApi gitHubApi,
    ILogger<RefitGitHubService> logger)
{
    public async Task<GitHubUserProfileDto?> GetUserProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        ApiResponse<GitHubUserProfileDto> response = await gitHubApi.GetUserProfileAsync(accessToken, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user profile. Status code: {StatusCode}", response.StatusCode);
            return null;
        }

        return response.Content;
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

        ApiResponse<List<GitHubEventDto>> response = await gitHubApi.GetUserEventsAsync(
            username,
            accessToken,
            page,
            perPage,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user events. Status code: {StatusCode}", response.StatusCode);
            return [];
        }

        return response.Content ?? [];
    }
}
