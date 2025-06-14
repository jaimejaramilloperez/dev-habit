using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevHabit.Api.Common.Pagination;
using DevHabit.Api.Dtos.Tags;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;

namespace DevHabit.IntegrationTests.Tests.TagsController;

public sealed class GetAllTagsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.TagRoutes.GetAll;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetTags_ShouldReturnEmptyList_WhenNoTagsExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<TagDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<TagDto>>();

        Assert.NotNull(result);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetTags_ShouldReturnTags_WhenTagsExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        CreateTagDto createDto = TagsTestData.ValidCreateTagDto;
        await client.PostAsJsonAsync(Routes.TagRoutes.Create, createDto);

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<TagDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<TagDto>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(createDto.Name, result.Data.First().Name);
    }

    [Fact]
    public async Task GetTags_ShouldSupportSearching_WhenSearchTermIsProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create tags first
        CreateTagDto[] tags =
        [
            TagsTestData.ValidCreateTagDto with { Name = "Programming", Description = "A Great description" },
            TagsTestData.ValidCreateTagDto with { Name = "Reading", Description = "A Great description" }
        ];

        await Task.WhenAll(tags.Select(tag => client.PostAsJsonAsync(Routes.TagRoutes.Create, tag)));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?q=prog", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<TagDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<TagDto>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("Programming", result.Data.First().Name);
    }

    [Fact]
    public async Task GetTags_ShouldSupportSorting_WhenSortParameterIsProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create tags first
        CreateTagDto[] tags =
        [
            TagsTestData.ValidCreateTagDto with { Name = "Programming" },
            TagsTestData.ValidCreateTagDto with { Name = "Reading" },
            TagsTestData.ValidCreateTagDto with { Name = "Fitness" }
        ];

        await Task.WhenAll(tags.Select(tag => client.PostAsJsonAsync(Routes.TagRoutes.Create, tag)));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?sort=name", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<TagDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<TagDto>>();

        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal("Fitness", result.Data.ElementAt(0).Name);
        Assert.Equal("Programming", result.Data.ElementAt(1).Name);
        Assert.Equal("Reading", result.Data.ElementAt(2).Name);
    }

    [Fact]
    public async Task GetTags_ShouldSupportDataShaping_WhenFieldsAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a tag first
        CreateTagDto habit = TagsTestData.ValidCreateTagDto;
        await client.PostAsJsonAsync(Routes.TagRoutes.Create, habit);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?fields=id,name", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<ExpandoObject>? result = await response.Content.ReadFromJsonAsync<PaginationResult<ExpandoObject>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);

        string json = JsonSerializer.Serialize(result.Data.First());
        Dictionary<string, object?>? item = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

        Assert.NotNull(item);
        Assert.Equal(2, item.Count);
        Assert.True(item.ContainsKey("id"));
        Assert.True(item.ContainsKey("name"));
        Assert.False(item.ContainsKey("description"));
        Assert.False(item.ContainsKey("createdAtUtc"));
    }

    [Fact]
    public async Task GetTags_ShouldSupportPagination_WhenPaginationParametersAreProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create tags first
        CreateTagDto[] tags =
        [
            TagsTestData.ValidCreateTagDto with { Name = "Programming" },
            TagsTestData.ValidCreateTagDto with { Name = "Reading" },
            TagsTestData.ValidCreateTagDto with { Name = "Fitness" }
        ];

        await Task.WhenAll(tags.Select(Tag => client.PostAsJsonAsync(Routes.TagRoutes.Create, Tag)));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?page=1&page_size=2", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<TagDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<TagDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
    }
}
