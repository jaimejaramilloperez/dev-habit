using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Users;
using DevHabit.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.UsersController;

public sealed class UpdateProfileUsersControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.UserRoutes.UpdateProfile;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task UpdateProfile_ShouldSucceed_WhenParametersAreValid()
    {
        // Arrange
        const string name = "test-user";
        HttpClient client = await CreateAuthenticatedClientAsync(name: name);

        UpdateProfileDto dto = new()
        {
            Name = "update-test-user",
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ShouldUpdateUserProfile_WhenUserIsAuthenticated()
    {
        // Arrange
        const string name = "test-user";
        HttpClient client = await CreateAuthenticatedClientAsync(name: name);

        UpdateProfileDto dto = new()
        {
            Name = "update-test-user",
        };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(EndpointRoute, dto);
        response.EnsureSuccessStatusCode();

        // Assert
        HttpResponseMessage userResponse = await client.GetAsync(new Uri(Routes.UserRoutes.CurrentUser, UriKind.Relative));
        UserDto? user = await userResponse.Content.ReadFromJsonAsync<UserDto>();

        Assert.NotNull(user);
        Assert.NotEqual(name, user.Name);
        Assert.Equal(dto.Name, user.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData($"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")]
    public async Task UpdateProfile_ShouldFail_WhenParametersAreInValid(string name)
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        UpdateProfileDto dto = new()
        {
            Name = name,
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
