using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.GitHubController;

public sealed class GetProfileGitHubControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory)
{
    private const string EndpointRoute = Routes.GitHubRoutes.GetProfile;

    [Fact]
    public async Task GetProfile_ShouldSucceed_WhenAccessTokenIsValid()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        StoreGithubAccessTokenDto dto = new()
        {
            AccessToken = GitHubConstants.ValidGitHubAccessToken,
            ExpiresInDays = 30,
        };

        await client.PutAsJsonAsync(Routes.GitHubRoutes.StorePersonalAccessToken, dto);

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_ShouldReturnUserProfile_WhenAccessTokenIsValid()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        StoreGithubAccessTokenDto dto = new()
        {
            AccessToken = GitHubConstants.ValidGitHubAccessToken,
            ExpiresInDays = 30,
        };

        await client.PutAsJsonAsync(Routes.GitHubRoutes.StorePersonalAccessToken, dto);

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));
        response.EnsureSuccessStatusCode();

        // Assert
        GitHubUserProfileDto? profile = await response.Content.ReadFromJsonAsync<GitHubUserProfileDto>();
        Assert.NotNull(profile);
    }

    [Fact]
    public async Task GetProfile_ShouldFail_WhenAccessTokenDoesNotExist()
    {
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
