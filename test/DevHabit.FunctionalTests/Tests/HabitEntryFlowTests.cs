using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Common.Pagination;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Dtos.Habits;
using DevHabit.FunctionalTests.Infrastructure;
using DevHabit.FunctionalTests.TestData;

namespace DevHabit.FunctionalTests.Tests;

public sealed class HabitEntryFlowTests(DevHabitWebAppFactory appFactory)
    : FunctionalTestFixture(appFactory), IAsyncLifetime
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task CompleteHabitEntryFlow_ShouldSucceed()
    {
        // Arrange
        const string email = "entryflow@test.com";
        const string password = "Test123!";

        HttpClient client = CreateClient();

        // Act

        // Step 1: Register a new user
        RegisterUserDto registerUserDto = new()
        {
            Name = email,
            Email = email,
            Password = password,
            ConfirmationPassword = password,
        };

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Register, registerUserDto);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Step 2: Login to get the token
        LoginUserDto loginUserDto = new()
        {
            Email = email,
            Password = password,
        };

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Login, loginUserDto);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        AccessTokensDto? accessTokens = await loginResponse.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(accessTokens);

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessTokens.AccessToken);

        // Step 3: Create a habit
        CreateHabitDto createHabitDto = HabitsTestData.CreateReadingHabitDto();
        HttpResponseMessage createHabitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        Assert.Equal(HttpStatusCode.Created, createHabitResponse.StatusCode);

        HabitDto? createdHabit = await createHabitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        // Step 4: Create first entry
        CreateEntryDto createFirstEntryDto = EntriesTestData.CreateEntryDto(
            habitId: createdHabit.Id,
            date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            value: 25,
            notes: "First reading season");

        client.DefaultRequestHeaders.Add(IdempotencyKeyHeader, Guid.NewGuid().ToString());
        HttpResponseMessage createFirstEntryResponse = await client.PostAsJsonAsync(Routes.EntryRoutes.Create, createFirstEntryDto);
        Assert.Equal(HttpStatusCode.Created, createFirstEntryResponse.StatusCode);
        client.DefaultRequestHeaders.Remove(IdempotencyKeyHeader);

        EntryDto? createdFirstEntry = await createFirstEntryResponse.Content.ReadFromJsonAsync<EntryDto>();
        Assert.NotNull(createdFirstEntry);
        Assert.Equal(25, createdFirstEntry.Value);

        // Step 5: Create second entry for the next day
        CreateEntryDto createSecondEntryDto = EntriesTestData.CreateEntryDto(
            habitId: createdHabit.Id,
            value: 35,
            notes: "Second reading season");

        client.DefaultRequestHeaders.Add(IdempotencyKeyHeader, Guid.NewGuid().ToString());
        HttpResponseMessage createSecondEntryResponse = await client.PostAsJsonAsync(Routes.EntryRoutes.Create, createSecondEntryDto);
        Assert.Equal(HttpStatusCode.Created, createSecondEntryResponse.StatusCode);
        client.DefaultRequestHeaders.Remove(IdempotencyKeyHeader);

        EntryDto? createdSecondEntry = await createSecondEntryResponse.Content.ReadFromJsonAsync<EntryDto>();
        Assert.NotNull(createdSecondEntry);
        Assert.Equal(35, createdSecondEntry.Value);

        // Step 6: Get all entries and verify
        HttpResponseMessage getEntriesResponse = await client.GetAsync(
            new Uri($"{Routes.EntryRoutes.GetAll}?habit_id={createdHabit.Id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, getEntriesResponse.StatusCode);

        PaginationResult<EntryDto>? entries = await getEntriesResponse.Content.ReadFromJsonAsync<PaginationResult<EntryDto>>();
        Assert.NotNull(entries);
        Assert.Equal(2, entries.Data.Count);

        // Step 7: Get entry statistics
        HttpResponseMessage getStatsResponse = await client.GetAsync(new Uri(Routes.EntryRoutes.Stats, UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, getStatsResponse.StatusCode);

        EntryStatsDto? stats = await getStatsResponse.Content.ReadFromJsonAsync<EntryStatsDto>();
        Assert.NotNull(stats);
        Assert.Equal(2, stats.TotalEntries);
    }
}
