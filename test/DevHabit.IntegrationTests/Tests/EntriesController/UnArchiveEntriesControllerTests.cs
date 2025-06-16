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

public sealed class UnArchiveEntriesControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task UnArchiveEntry_ShouldSucceed_WhenParametersAreValid()
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
        HttpResponseMessage response = await client.PutAsync(
            new Uri(Routes.EntryRoutes.UnArchive(createdEntry.Id), UriKind.Relative), null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify entry was updated
        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.EntryRoutes.Get}/{createdEntry.Id}", UriKind.Relative));

        getResponse.EnsureSuccessStatusCode();

        EntryDto? result = await getResponse.Content.ReadFromJsonAsync<EntryDto>();

        Assert.NotNull(result);
        Assert.False(result.IsArchived);
    }

    [Fact]
    public async Task UnArchiveEntry_ShouldReturnNotFound_WhenEntryDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.PutAsync(
            new Uri(Routes.EntryRoutes.UnArchive(Entry.CreateNewId()), UriKind.Relative), null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }
}
