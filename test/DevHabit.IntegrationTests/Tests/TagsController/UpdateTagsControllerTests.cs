using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.TagsController;

public sealed class UpdateTagControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.TagRoutes.Update;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task UpdateTag_ShouldSucceed_WhenParametersAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a tag first
        CreateTagDto createDto = TagsTestData.ValidCreateTagDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        TagDto? createdTag = await createResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(createdTag);

        UpdateTagDto updateDto = TagsTestData.ValidUpdateTagDto;

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri($"{EndpointRoute}/{createdTag.Id}", UriKind.Relative), updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify tag was updated
        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.TagRoutes.Get}/{createdTag.Id}", UriKind.Relative));

        TagDto? result = await getResponse.Content.ReadFromJsonAsync<TagDto>();

        Assert.NotNull(result);
        Assert.Equal(createdTag.Id, result.Id);
        Assert.Equal(updateDto.Name, result.Name);
        Assert.Equal(updateDto.Description, result.Description);
    }

    [Fact]
    public async Task UpdateTag_ShouldFail_WhenParametersAreInValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a tag first
        CreateTagDto createDto = TagsTestData.ValidCreateTagDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        TagDto? createdTag = await createResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(createdTag);

        UpdateTagDto updateDto = TagsTestData.InvalidUpdateTagDto;

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri($"{EndpointRoute}/{createdTag.Id}", UriKind.Relative), updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Single(problem.Errors);
    }

    [Fact]
    public async Task UpdateTag_ShouldReturnNotFound_WhenTagDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        UpdateTagDto updateDto = TagsTestData.ValidUpdateTagDto;

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri($"{EndpointRoute}/{Tag.CreateNewId()}", UriKind.Relative), updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }

    [Fact]
    public async Task UpdateTag_ShouldEnforceDuplicateNameValidation_WhenUpdatingToExistingName()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create first tag
        CreateTagDto firstTag = TagsTestData.ValidCreateTagDto;
        HttpResponseMessage firstTagResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, firstTag);
        firstTagResponse.EnsureSuccessStatusCode();

        // Create second tag
        CreateTagDto secondTag = TagsTestData.ValidCreateTagDto with { Name = "Second Tag" };
        HttpResponseMessage secondTagResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, secondTag);
        secondTagResponse.EnsureSuccessStatusCode();

        TagDto? createdSecondTag = await secondTagResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(createdSecondTag);

        // Try to update second tag with first tag's name
        UpdateTagDto updateDto = new() { Name = firstTag.Name, Description = "Updated description" };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri($"{EndpointRoute}/{createdSecondTag.Id}", UriKind.Relative), updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal($"The tag '{updateDto.Name}' already exists", problem.Detail);
        Assert.Empty(problem.Errors);
    }
}
