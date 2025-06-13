using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Auth;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.AuthController;

public sealed class RegisterAuthControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.AuthRoutes.Register;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task Register_ShouldSucceed_WhenParametersAreValid()
    {
        // Arrange
        HttpClient client = CreateClient();
        RegisterUserDto dto = AuthTestData.ValidRegisterUserDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShouldReturnAccessTokens_WhenParametersAreValid()
    {
        // Arrange
        HttpClient client = CreateClient();
        RegisterUserDto dto = AuthTestData.ValidRegisterUserDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);
        response.EnsureSuccessStatusCode();

        // Assert
        AccessTokensDto? accessTokensDto = await response.Content.ReadFromJsonAsync<AccessTokensDto>();

        Assert.NotNull(accessTokensDto);
        Assert.NotNull(accessTokensDto.AccessToken);
        Assert.NotEmpty(accessTokensDto.AccessToken);
        Assert.NotNull(accessTokensDto.RefreshToken);
        Assert.NotEmpty(accessTokensDto.RefreshToken);
    }

    [Fact]
    public async Task Register_ShouldFail_WhenParametersHaveADuplicateEmail()
    {
        // Arrange
        HttpClient client = CreateClient();

        RegisterUserDto dto = AuthTestData.ValidRegisterUserDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(EndpointRoute, dto);
        createResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal($"Email '{dto.Email}' is already taken", problem.Detail);
        Assert.Empty(problem.Errors);
    }

    [Theory]
    [InlineData("", "test@example.com", "Test123!", "Test123!")]
    [InlineData("Test User", "", "Test123!", "Test123!")]
    [InlineData("Test User", "invalid-email", "Test123!", "Test123!")]
    [InlineData("Test User", "test@example.com", "", "Test123!")]
    [InlineData("Test User", "test@example.com", "Test123!", "")]
    [InlineData("Test User", "test@example.com", "Test123!", "DifferentPass!")]
    [InlineData("Test User", "test@example.com", "weak", "weak")]
    public async Task Register_ShouldFail_WhenParametersAreInvalid(
        string name,
        string email,
        string password,
        string confirmPassword)
    {
        // Arrange
        HttpClient client = CreateClient();

        RegisterUserDto dto = new()
        {
            Name = name,
            Email = email,
            Password = password,
            ConfirmationPassword = confirmPassword,
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
