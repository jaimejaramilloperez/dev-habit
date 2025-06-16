using System.Dynamic;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DevHabit.Api.Common.Pagination;
using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Dtos.Habits;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;

namespace DevHabit.IntegrationTests.Tests.EntriesController;

public sealed class GetAllEntriesControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.EntryRoutes.GetAll;
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetEntries_ShouldReturnEmptyList_WhenNoHabitsExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<EntryDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<EntryDto>>();

        Assert.NotNull(result);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetEntries_ShouldReturnEntries_WhenEntriesExists()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit
        CreateHabitDto createHabitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createdResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        createdResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createdResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        // Create an entry
        CreateEntryDto createDto = EntriesTestData.ValidCreateEntryDto with { HabitId = createdHabit.Id };
        client.DefaultRequestHeaders.Add(IdempotencyKeyHeader, Guid.NewGuid().ToString());

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.EntryRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        EntryDto? createdEntry = await createResponse.Content.ReadFromJsonAsync<EntryDto>();
        Assert.NotNull(createdEntry);

        client.DefaultRequestHeaders.Remove(IdempotencyKeyHeader);

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<EntryDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<EntryDto>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(createdEntry.Id, result.Data.First().Id);
    }

    [Fact]
    public async Task GetEntries_ShouldSupportFiltering_WhenOneFilterParameterIsProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit
        CreateHabitDto createHabitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createdResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        createdResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createdResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        // Create entries
        CreateEntryDto[] entries =
        [
            EntriesTestData.ValidCreateEntryDto with {
                HabitId = createdHabit.Id,
                Date = DateOnly.ParseExact("2024-12-06", "yyyy-MM-dd", CultureInfo.InvariantCulture),
            },
            EntriesTestData.ValidCreateEntryDto with {
                HabitId = createdHabit.Id,
                Date = DateOnly.ParseExact("2025-01-12", "yyyy-MM-dd", CultureInfo.InvariantCulture),
            },
            EntriesTestData.ValidCreateEntryDto with {
                HabitId = createdHabit.Id,
                Date = DateOnly.ParseExact("2025-01-25", "yyyy-MM-dd", CultureInfo.InvariantCulture),
            },
        ];

        await Task.WhenAll(entries.Select(entry =>
        {
            client.DefaultRequestHeaders.Remove(IdempotencyKeyHeader);
            client.DefaultRequestHeaders.Add(IdempotencyKeyHeader, Guid.NewGuid().ToString());
            return client.PostAsJsonAsync(Routes.EntryRoutes.Create, entry);
        }));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?from_date=2025-01-01&to_date=2025-02-01", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<EntryDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<EntryDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2025, result.Data.ElementAt(0).Date.Year);
        Assert.Equal(2025, result.Data.ElementAt(1).Date.Year);
    }

    [Fact]
    public async Task GetEntries_ShouldFilterByHabitId_WhenHabitIdFilterParameterIsProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create first habit
        CreateHabitDto createFirstHabitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createdFirstHabitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createFirstHabitDto);
        createdFirstHabitResponse.EnsureSuccessStatusCode();

        HabitDto? createdFirstHabit = await createdFirstHabitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdFirstHabit);

        // Create second habit
        CreateHabitDto createSecondHabitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createdSecondHabitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createSecondHabitDto);
        createdSecondHabitResponse.EnsureSuccessStatusCode();

        HabitDto? createdSecondHabit = await createdSecondHabitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdSecondHabit);

        // Create entries
        CreateEntryDto[] entries =
        [
            EntriesTestData.ValidCreateEntryDto with { HabitId = createdFirstHabit.Id },
            EntriesTestData.ValidCreateEntryDto with { HabitId = createdSecondHabit.Id },
        ];

        await Task.WhenAll(entries.Select(entry =>
        {
            client.DefaultRequestHeaders.Remove(IdempotencyKeyHeader);
            client.DefaultRequestHeaders.Add(IdempotencyKeyHeader, Guid.NewGuid().ToString());
            return client.PostAsJsonAsync(Routes.EntryRoutes.Create, entry);
        }));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?habit_id={createdSecondHabit.Id}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<EntryDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<EntryDto>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GetEntries_ShouldSupportSorting_WhenSortParameterIsProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit
        CreateHabitDto createHabitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createdResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        createdResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createdResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        // Create entries
        CreateEntryDto[] entries =
        [
            EntriesTestData.ValidCreateEntryDto with { HabitId = createdHabit.Id, Value = 25 },
            EntriesTestData.ValidCreateEntryDto with { HabitId = createdHabit.Id, Value = 15},
            EntriesTestData.ValidCreateEntryDto with { HabitId = createdHabit.Id, Value = 5 },
        ];

        await Task.WhenAll(entries.Select(entry =>
        {
            client.DefaultRequestHeaders.Remove(IdempotencyKeyHeader);
            client.DefaultRequestHeaders.Add(IdempotencyKeyHeader, Guid.NewGuid().ToString());
            return client.PostAsJsonAsync(Routes.EntryRoutes.Create, entry);
        }));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?sort=value", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<EntryDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<EntryDto>>();

        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Equal(5, result.Data.ElementAt(0).Value);
        Assert.Equal(15, result.Data.ElementAt(1).Value);
        Assert.Equal(25, result.Data.ElementAt(2).Value);
    }

    [Fact]
    public async Task GetEntries_ShouldSupportDataShaping_WhenFieldsAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit
        CreateHabitDto createHabitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createdResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        createdResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createdResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        // Create an entry
        CreateEntryDto createDto = EntriesTestData.ValidCreateEntryDto with { HabitId = createdHabit.Id };
        client.DefaultRequestHeaders.Add(IdempotencyKeyHeader, Guid.NewGuid().ToString());

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.EntryRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        EntryDto? createdEntry = await createResponse.Content.ReadFromJsonAsync<EntryDto>();
        Assert.NotNull(createdEntry);

        client.DefaultRequestHeaders.Remove(IdempotencyKeyHeader);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?fields=id,value,date", UriKind.Relative));

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
        Assert.True(item.ContainsKey("value"));
        Assert.True(item.ContainsKey("date"));
        Assert.False(item.ContainsKey("notes"));
        Assert.False(item.ContainsKey("source"));
        Assert.False(item.ContainsKey("createdAtUtc"));
    }

    [Fact]
    public async Task GetEntries_ShouldSupportPagination_WhenPaginationParametersAreProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit
        CreateHabitDto createHabitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createdResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        createdResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createdResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        // Create entries
        CreateEntryDto[] entries =
        [
            EntriesTestData.ValidCreateEntryDto with { HabitId = createdHabit.Id, Value = 25 },
            EntriesTestData.ValidCreateEntryDto with { HabitId = createdHabit.Id, Value = 15},
            EntriesTestData.ValidCreateEntryDto with { HabitId = createdHabit.Id, Value = 5 },
        ];

        await Task.WhenAll(entries.Select(entry =>
        {
            client.DefaultRequestHeaders.Remove(IdempotencyKeyHeader);
            client.DefaultRequestHeaders.Add(IdempotencyKeyHeader, Guid.NewGuid().ToString());
            return client.PostAsJsonAsync(Routes.EntryRoutes.Create, entry);
        }));

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?page=1&page_size=2", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<EntryDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<EntryDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
    }
}
