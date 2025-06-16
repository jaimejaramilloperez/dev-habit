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

public sealed class UpdateEntriesControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.EntryRoutes.Update;
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task UpdateEntry_ShouldSucceed_WhenParametersAreValid()
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

        UpdateEntryDto updateDto = EntriesTestData.ValidUpdateEntryDto;

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri($"{EndpointRoute}/{createdEntry.Id}", UriKind.Relative), updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify entry was updated
        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.EntryRoutes.Get}/{createdEntry.Id}", UriKind.Relative));

        EntryDto? result = await getResponse.Content.ReadFromJsonAsync<EntryDto>();

        Assert.NotNull(result);
        Assert.Equal(createdEntry.Id, result.Id);
        Assert.Equal(updateDto.Value, result.Value);
        Assert.Equal(updateDto.Notes, result.Notes);
    }

    [Fact]
    public async Task UpdateEntry_ShouldReturnFail_WhenParametersAreInValid()
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

        UpdateEntryDto updateDto = EntriesTestData.InvalidUpdateEntryDto;

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri($"{EndpointRoute}/{createdEntry.Id}", UriKind.Relative), updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Single(problem.Errors);
    }

    [Fact]
    public async Task UpdateEntry_ShouldReturnNotFound_WhenEntryDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        UpdateEntryDto updateDto = EntriesTestData.ValidUpdateEntryDto;

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri($"{EndpointRoute}/{Entry.CreateNewId()}", UriKind.Relative), updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }
}
