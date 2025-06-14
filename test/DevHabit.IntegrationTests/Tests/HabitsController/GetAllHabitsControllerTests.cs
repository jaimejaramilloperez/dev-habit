using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevHabit.Api.Common.Pagination;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;

namespace DevHabit.IntegrationTests.Tests.HabitsController;

public sealed class GetAllHabitsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.HabitRoutes.GetAll;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetHabits_ShouldReturnEmptyList_WhenNoHabitsExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();

        Assert.NotNull(result);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetHabits_ShouldReturnHabits_WhenHabitsExists()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        CreateHabitDto createDto = HabitsTestData.ValidCreateHabitDto;
        await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createDto);

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(createDto.Name, result.Data.First().Name);
    }

    [Fact]
    public async Task GetHabits_ShouldSupportSearching_WhenSearchTermIsProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create habits first
        CreateHabitDto[] habits =
        [
            HabitsTestData.ValidCreateHabitDto with { Name = "Read Books", Description = "A Great description" },
            HabitsTestData.ValidCreateHabitDto with { Name = "Exercise Daily", Description = "A Great description" }
        ];

        await Task.WhenAll(habits.Select(habit => client.PostAsJsonAsync(Routes.HabitRoutes.Create, habit)));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?q=read", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("Read Books", result.Data.First().Name);
    }

    [Fact]
    public async Task GetHabits_ShouldSupportFiltering_WhenOneFilterParameterIsProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create habits first
        CreateHabitDto measurableHabit = HabitsTestData.ValidCreateHabitDto;
        CreateHabitDto binaryHabit = measurableHabit with { Type = HabitType.Binary };

        await client.PostAsJsonAsync(Routes.HabitRoutes.Create, measurableHabit);
        await client.PostAsJsonAsync(Routes.HabitRoutes.Create, binaryHabit);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?type={(int)HabitType.Measurable}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(HabitType.Measurable, result.Data.First().Type);
    }

    [Fact]
    public async Task GetHabits_ShouldSupportSorting_WhenSortParameterIsProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create habits first
        CreateHabitDto[] habits =
        [
            HabitsTestData.ValidCreateHabitDto with { Name = "Read Books" },
            HabitsTestData.ValidCreateHabitDto with { Name = "Exercise Daily" },
            HabitsTestData.ValidCreateHabitDto with { Name = "Meditate" },
        ];

        await Task.WhenAll(habits.Select(habit => client.PostAsJsonAsync(Routes.HabitRoutes.Create, habit)));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?sort=name", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();

        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal("Exercise Daily", result.Data.ElementAt(0).Name);
        Assert.Equal("Meditate", result.Data.ElementAt(1).Name);
        Assert.Equal("Read Books", result.Data.ElementAt(2).Name);
    }

    [Fact]
    public async Task GetHabits_ShouldSupportDataShaping_WhenFieldsAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto habit = HabitsTestData.ValidCreateHabitDto;
        await client.PostAsJsonAsync(Routes.HabitRoutes.Create, habit);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?fields=id,name,type", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<ExpandoObject>? result = await response.Content.ReadFromJsonAsync<PaginationResult<ExpandoObject>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);

        string json = JsonSerializer.Serialize(result.Data.First());
        Dictionary<string, object?>? item = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

        Assert.NotNull(item);
        Assert.Equal(3, item.Count);
        Assert.True(item.ContainsKey("id"));
        Assert.True(item.ContainsKey("name"));
        Assert.True(item.ContainsKey("type"));
        Assert.False(item.ContainsKey("description"));
        Assert.False(item.ContainsKey("status"));
        Assert.False(item.ContainsKey("createdAtUtc"));
    }

    [Fact]
    public async Task GetHabits_ShouldSupportPagination_WhenPaginationParametersAreProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create habits first
        CreateHabitDto[] habits =
        [
            HabitsTestData.ValidCreateHabitDto with { Name = "Read Books" },
            HabitsTestData.ValidCreateHabitDto with { Name = "Exercise Daily" },
            HabitsTestData.ValidCreateHabitDto with { Name = "Meditate" },
        ];

        await Task.WhenAll(habits.Select(habit => client.PostAsJsonAsync(Routes.HabitRoutes.Create, habit)));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?page=1&page_size=2", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<HabitDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<HabitDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
    }
}
