using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.GitHubController;

public sealed class GetUserEventsGitHubControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.GitHubRoutes.GetUserEvents;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetUserEvents_ShouldSucceed_WhenAccessTokenIsValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        StoreGithubAccessTokenDto storeDto = new()
        {
            AccessToken = GitHubConstants.ValidGitHubAccessToken,
            ExpiresInDays = 30,
        };

        await client.PutAsJsonAsync(Routes.GitHubRoutes.StorePersonalAccessToken, storeDto);

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserEvents_ShouldReturnUserProfile_WhenAccessTokenIsValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        StoreGithubAccessTokenDto storeDto = new()
        {
            AccessToken = GitHubConstants.ValidGitHubAccessToken,
            ExpiresInDays = 30,
        };

        await client.PutAsJsonAsync(Routes.GitHubRoutes.StorePersonalAccessToken, storeDto);

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));
        response.EnsureSuccessStatusCode();

        // Assert
        GitHubEventDto[]? result = await response.Content.ReadFromJsonAsync<GitHubEventDto[]>();
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetUserEvents_ShouldReturnNotFound_WhenAccessTokenDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }
}
