using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Dtos.Habits;
using DevHabit.FunctionalTests.Infrastructure;
using DevHabit.FunctionalTests.Services;
using DevHabit.FunctionalTests.TestData;

namespace DevHabit.FunctionalTests.Tests;

public sealed class GitHubIntegrationFlowTests(DevHabitWebAppFactory appFactory)
    : FunctionalTestFixture(appFactory), IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task CompleteGitHubIntegrationFlow_ShouldSucceed()
    {
        // Arrange
        const string email = "githubflow@test.com";
        const string password = "Test123!";

        HttpClient client = CreateClient();

        // Act

        // Step 1: Register a new user
        RegisterUserDto registerUserDto = new()
        {
            Name = email,
            Email = email,
            Password = password,
            ConfirmationPassword = password,
        };

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Register, registerUserDto);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Step 2: Login to get the token
        LoginUserDto loginUserDto = new()
        {
            Email = email,
            Password = password,
        };

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Login, loginUserDto);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        AccessTokensDto? accessTokens = await loginResponse.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(accessTokens);

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessTokens.AccessToken);

        // Step 3: Create a habit
        CreateHabitDto createHabitDto = HabitsTestData.CreateReadingHabitDto();
        HttpResponseMessage createHabitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        Assert.Equal(HttpStatusCode.Created, createHabitResponse.StatusCode);

        HabitDto? createdHabit = await createHabitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        // Step 4: Store GitHub access token
        StoreGithubAccessTokenDto storeGithubAccessTokenDto = new()
        {
            AccessToken = GitHubConstants.GitHubAccessToken,
            ExpiresInDays = 30,
        };

        HttpResponseMessage storeAccessTokenResponse = await client.PutAsJsonAsync(
            Routes.GitHubRoutes.StorePersonalAccessToken, storeGithubAccessTokenDto);

        Assert.Equal(HttpStatusCode.NoContent, storeAccessTokenResponse.StatusCode);

        // Step 5: Get GitHub profile
        HttpResponseMessage getGitHubProfileResponse = await client.GetAsync(
            new Uri(Routes.GitHubRoutes.GetUserProfile, UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, getGitHubProfileResponse.StatusCode);

        GitHubUserProfileDto? gitHubUserProfile = await getGitHubProfileResponse.Content.ReadFromJsonAsync<GitHubUserProfileDto>();
        Assert.NotNull(gitHubUserProfile);
        Assert.Equal(GitHubConstants.GitHubUser, gitHubUserProfile.Login);
        Assert.Equal(GitHubConstants.GitHubUser, gitHubUserProfile.Name);
        Assert.Equal($"{GitHubConstants.GitHubUser} bio", gitHubUserProfile.Bio);

        // Step 6: Revoke GitHub access token
        HttpResponseMessage revokeAccessTokenResponse = await client.DeleteAsync(
            new Uri(Routes.GitHubRoutes.RevokePersonalAccessToken, UriKind.Relative));

        Assert.Equal(HttpStatusCode.NoContent, revokeAccessTokenResponse.StatusCode);

        // Step 7: Verify profile access is revoked
        HttpResponseMessage getGitHubProfileAfterRevokeResponse = await client.GetAsync(
            new Uri(Routes.GitHubRoutes.GetUserProfile, UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, getGitHubProfileAfterRevokeResponse.StatusCode);
    }
}
