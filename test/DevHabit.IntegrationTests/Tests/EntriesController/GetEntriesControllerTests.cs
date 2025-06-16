using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.EntriesController;

public sealed class GetEntriesControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.EntryRoutes.Get;
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetEntry_ShouldReturnEntry_WhenEntryExists()
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
            new Uri($"{EndpointRoute}/{createdEntry.Id}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        EntryDto? result = await response.Content.ReadFromJsonAsync<EntryDto>();

        Assert.NotNull(result);
        Assert.Equal(createdEntry.Id, result.Id);
    }

    [Fact]
    public async Task GetEntry_ShouldReturnNotFound_WhenEntryDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}/{Entry.CreateNewId()}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }

    [Fact]
    public async Task GetEntry_ShouldSupportDataShaping_WhenFieldsAreValid()
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
            new Uri($"{EndpointRoute}/{createdEntry.Id}?fields=id,value,date", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ExpandoObject? result = await response.Content.ReadFromJsonAsync<ExpandoObject>();
        Assert.NotNull(result);

        IDictionary<string, object?> item = result;

        Assert.Equal(3, item.Count);
        Assert.True(item.ContainsKey("id"));
        Assert.True(item.ContainsKey("value"));
        Assert.True(item.ContainsKey("date"));
        Assert.False(item.ContainsKey("notes"));
        Assert.False(item.ContainsKey("source"));
        Assert.False(item.ContainsKey("createdAtUtc"));
    }
}
