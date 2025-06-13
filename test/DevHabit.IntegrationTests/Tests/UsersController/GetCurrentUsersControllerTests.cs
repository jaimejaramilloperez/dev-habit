using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Users;
using DevHabit.IntegrationTests.Infrastructure;

namespace DevHabit.IntegrationTests.Tests.UsersController;

public sealed class GetCurrentUsersControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.UserRoutes.CurrentUser;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetCurrentUser_ShouldSucceed_WhenUserIsAuthenticated()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUser_WhenUserIsAuthenticated()
    {
        // Arrange
        const string email = "test@example.com";
        HttpClient client = await CreateAuthenticatedClientAsync(email);

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));
        response.EnsureSuccessStatusCode();

        // Assert
        UserDto? userDto = await response.Content.ReadFromJsonAsync<UserDto>();

        Assert.NotNull(userDto);
        Assert.Equal(email, userDto.Email);
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        HttpClient client = CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
