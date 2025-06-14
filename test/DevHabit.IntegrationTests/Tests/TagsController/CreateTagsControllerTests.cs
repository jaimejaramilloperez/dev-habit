using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Tags;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.TagsController;

public sealed class CreateTagControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.TagRoutes.Create;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task CreateTag_ShouldSucceed_WhenParametersAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateTagDto createDto = TagsTestData.ValidCreateTagDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.NotNull(locationHeader);

        TagDto? result = await response.Content.ReadFromJsonAsync<TagDto>();

        Assert.NotNull(result);
        Assert.Equal($"/{EndpointRoute}/{result.Id}", locationHeader.AbsolutePath);
        Assert.NotEmpty(result.Id);
    }

    [Fact]
    public async Task CreateTag_ShouldReturnError_WhenParametersAreInValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateTagDto createDto = TagsTestData.InvalidCreateTagDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.Null(locationHeader);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Single(problem.Errors);
    }

    [Fact]
    public async Task CreateTag_ShouldEnforceDuplicateNameValidation_WhenTagWithSameNameExists()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateTagDto createDto = TagsTestData.ValidCreateTagDto;

        // Create first tag
        await client.PostAsJsonAsync(EndpointRoute, createDto);

        // Act - Try to create tag with same name
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal($"The tag '{createDto.Name}' already exists", problem.Detail);
        Assert.Empty(problem.Errors);
    }
}
