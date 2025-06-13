using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.GitHubController;

public sealed class StoreAccessTokenGitHubControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.GitHubRoutes.StorePersonalAccessToken;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task StoreAccessToken_ShouldSucceed_WhenParametersAreValid()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        StoreGithubAccessTokenDto dto = new()
        {
            AccessToken = GitHubConstants.ValidGitHubAccessToken,
            ExpiresInDays = 30,
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData("", 5)]
    [InlineData("token", 0)]
    public async Task StoreAccessToken_ShouldFail_WhenParametersAreNotValid(
        string accessToken,
        int expiresInDays)
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        StoreGithubAccessTokenDto dto = new()
        {
            AccessToken = accessToken,
            ExpiresInDays = expiresInDays,
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Single(problem.Errors);
    }
}
