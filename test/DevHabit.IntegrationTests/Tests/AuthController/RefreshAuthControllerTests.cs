using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Auth;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.AuthController;

public sealed class RefreshAuthControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.AuthRoutes.Refresh;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task Refresh_ShouldSucceed_WhenTokenIsValid()
    {
        // Arrange
        HttpClient client = CreateClient();

        RegisterUserDto registerDto = AuthTestData.ValidRegisterUserDto;
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Register, registerDto);
        registerResponse.EnsureSuccessStatusCode();

        AccessTokensDto? initialTokens = await registerResponse.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(initialTokens);

        RefreshTokenDto dto = new(initialTokens.RefreshToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ShouldReturnAccessTokens_WhenTokenIsValid()
    {
        // Arrange
        HttpClient client = CreateClient();

        RegisterUserDto registerDto = AuthTestData.ValidRegisterUserDto;
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Register, registerDto);
        registerResponse.EnsureSuccessStatusCode();

        AccessTokensDto? initialTokens = await registerResponse.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(initialTokens);

        RefreshTokenDto dto = new(initialTokens.RefreshToken);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);
        response.EnsureSuccessStatusCode();

        // Assert
        AccessTokensDto? tokens = await response.Content.ReadFromJsonAsync<AccessTokensDto>();

        Assert.NotNull(tokens);
        Assert.NotNull(tokens.AccessToken);
        Assert.NotEmpty(tokens.AccessToken);
        Assert.NotNull(tokens.RefreshToken);
        Assert.NotEmpty(tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ShouldIssueNewValidTokens_WhenTokenIsValid()
    {
        // Arrange
        HttpClient client = CreateClient();

        RegisterUserDto registerDto = AuthTestData.ValidRegisterUserDto;
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Register, registerDto);
        registerResponse.EnsureSuccessStatusCode();

        AccessTokensDto? initialTokens = await registerResponse.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(initialTokens);

        // Act
        RefreshTokenDto firstRefreshDto = new(initialTokens.RefreshToken);
        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(EndpointRoute, firstRefreshDto);
        firstResponse.EnsureSuccessStatusCode();

        AccessTokensDto? firstRefreshTokensDto = await firstResponse.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(firstRefreshTokensDto);

        RefreshTokenDto secondRefreshDto = new(firstRefreshTokensDto.RefreshToken);
        HttpResponseMessage secondResponse = await client.PostAsJsonAsync(EndpointRoute, secondRefreshDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        AccessTokensDto? finalTokens = await secondResponse.Content.ReadFromJsonAsync<AccessTokensDto>();

        Assert.NotNull(finalTokens);
        Assert.NotNull(finalTokens.AccessToken);
        Assert.NotEmpty(finalTokens.AccessToken);
        Assert.NotNull(finalTokens.RefreshToken);
        Assert.NotEmpty(finalTokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_ShouldFail_WhenTokenIsInvalid()
    {
        // Arrange
        HttpClient client = CreateClient();
        RefreshTokenDto dto = new("invalid-token");

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
    [InlineData("")]
    [InlineData("   ")]
    public async Task Refresh_ShouldFail_WhenParametersAreInvalid(
        string refreshToken)
    {
        // Arrange
        HttpClient client = CreateClient();
        RefreshTokenDto dto = new(refreshToken);

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
