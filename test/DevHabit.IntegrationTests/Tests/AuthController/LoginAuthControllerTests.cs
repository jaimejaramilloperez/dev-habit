using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Auth;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.AuthController;

public sealed class LoginAuthControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.AuthRoutes.Login;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task Login_ShouldSucceed_WhenCredentialsAreValid()
    {
        // Arrange
        HttpClient client = CreateClient();

        RegisterUserDto registerDto = AuthTestData.ValidRegisterUserDto;
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Register, registerDto);
        registerResponse.EnsureSuccessStatusCode();

        LoginUserDto dto = AuthTestData.ValidLoginUserDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_ShouldReturnAccessTokens_WhenCredentialsAreValid()
    {
        // Arrange
        HttpClient client = CreateClient();

        RegisterUserDto registerDto = AuthTestData.ValidRegisterUserDto;
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Register, registerDto);
        registerResponse.EnsureSuccessStatusCode();

        LoginUserDto dto = AuthTestData.ValidLoginUserDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);

        // Assert
        AccessTokensDto? tokens = await response.Content.ReadFromJsonAsync<AccessTokensDto>();

        Assert.NotNull(tokens);
        Assert.NotNull(tokens.AccessToken);
        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotNull(tokens.RefreshToken);
        Assert.NotEmpty(tokens.RefreshToken);
    }

    [Fact]
    public async Task Login_ShouldFail_WhenCredentialsAreInvalid()
    {
        // Arrange
        HttpClient client = CreateClient();
        LoginUserDto dto = AuthTestData.ValidLoginUserDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
        Assert.Equal("Unauthorized", problem.Title);
        Assert.Empty(problem.Errors);
    }

    [Theory]
    [InlineData("", "Test123!")]
    [InlineData("test@example.com", "")]
    [InlineData("invalid-email", "Test123!")]
    public async Task Login_ShouldFail_WhenParametersAreInvalid(
        string email,
        string password)
    {
        // Arrange
        HttpClient client = CreateClient();

        LoginUserDto dto = new()
        {
            Email = email,
            Password = password,
        };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
    }
}
