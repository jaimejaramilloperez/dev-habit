using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.TagsController;

public sealed class GetTagControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.TagRoutes.Get;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetTag_ShouldReturnTag_WhenTagExists()
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
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}/{createdTag.Id}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        TagDto? result = await response.Content.ReadFromJsonAsync<TagDto>();

        Assert.NotNull(result);
        Assert.Equal(createdTag.Id, result.Id);
    }

    [Fact]
    public async Task GetTag_ShouldReturnNotFound_WhenTagDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}/{Tag.CreateNewId()}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }

    [Fact]
    public async Task GetTags_ShouldSupportDataShaping_WhenFieldsAreValid()
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
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}/{createdTag.Id}?fields=id,name", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ExpandoObject? result = await response.Content.ReadFromJsonAsync<ExpandoObject>();
        Assert.NotNull(result);

        IDictionary<string, object?> item = result;

        Assert.Equal(2, item.Count);
        Assert.True(item.ContainsKey("id"));
        Assert.True(item.ContainsKey("name"));
        Assert.False(item.ContainsKey("description"));
        Assert.False(item.ContainsKey("createdAtUtc"));
    }
}
