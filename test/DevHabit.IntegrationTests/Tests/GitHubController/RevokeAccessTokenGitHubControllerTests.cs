using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.Services;

namespace DevHabit.IntegrationTests.Tests.GitHubController;

public sealed class RevokeAccessTokenGitHubControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.GitHubRoutes.RevokePersonalAccessToken;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task RevokeAccessToken_ShouldSucceed_WhenAccessTokenExists()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        StoreGithubAccessTokenDto dto = new()
        {
            AccessToken = GitHubConstants.ValidGitHubAccessToken,
            ExpiresInDays = 30,
        };

        HttpResponseMessage storeResponse = await client.PutAsJsonAsync(Routes.GitHubRoutes.StorePersonalAccessToken, dto);
        storeResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await client.DeleteAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RevokeAccessToken_ShouldSucceed_WhenAccessTokenDoesNotExist()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.DeleteAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
