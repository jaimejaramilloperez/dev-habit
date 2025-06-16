using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.TagsController;

public sealed class DeleteTagControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.TagRoutes.Delete;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task DeleteTag_ShouldDeleteTag_WhenTagExists()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a tag first
        CreateTagDto createDto = TagsTestData.ValidCreateTagDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        TagDto? createdTag = await createResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(createdTag);

        // Act
        HttpResponseMessage response = await client.DeleteAsync(
            new Uri($"{EndpointRoute}/{createdTag.Id}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify tag was deleted
        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.TagRoutes.Get}/{createdTag.Id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTag_ShouldReturnNotFound_WhenTagDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.DeleteAsync(
            new Uri($"{EndpointRoute}/{Tag.CreateNewId()}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }
}
