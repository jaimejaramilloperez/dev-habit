using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using DevHabit.Api.Common.Pagination;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Dtos.Entries.ImportJob;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.FunctionalTests.Infrastructure;
using DevHabit.FunctionalTests.TestData;

namespace DevHabit.FunctionalTests.Tests;

public sealed class EntryImportFlowTests(DevHabitWebAppFactory appFactory)
    : FunctionalTestFixture(appFactory), IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task CompleteEntryImportFlow_ShouldSucceed()
    {
        // Arrange
        const string email = "importflow@test.com";
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
        Assert.Equal(createHabitDto.Name, createdHabit.Name);

        // Step 4: Create a CSV content for import
        string csvContent = $"""
            habit_id,date,notes
            {createdHabit.Id},2025-01-01,First day of reading
            {createdHabit.Id},2025-01-02,Second day of reading
            {createdHabit.Id},2025-01-03,Third day of reading
        """;

        // Step 5: Create and submit import job
        using MultipartFormDataContent content = [];
        using ByteArrayContent file = new(Encoding.UTF8.GetBytes(csvContent));
        file.Headers.ContentType = new(MediaTypeNames.Text.Csv);
        content.Add(file, "file", "entries.csv");

        HttpResponseMessage importResponse = await client.PostAsync(
            new Uri(Routes.EntryImportJobRoutes.Create, UriKind.Relative), content);

        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        EntryImportJobDto? importJobDto = await importResponse.Content.ReadFromJsonAsync<EntryImportJobDto>();
        Assert.NotNull(importJobDto);

        // Step 6: Wait for import job to complete (with timeout)
        const int maxAttempts = 10;
        const int delayInMs = 1000;
        EntryImportJobDto? completedJob = null;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            await Task.Delay(delayInMs);

            HttpResponseMessage jobStatusResponse = await client.GetAsync(
                new Uri($"{Routes.EntryImportJobRoutes.Get}/{importJobDto.Id}", UriKind.Relative));

            Assert.Equal(HttpStatusCode.OK, jobStatusResponse.StatusCode);

            completedJob = await jobStatusResponse.Content.ReadFromJsonAsync<EntryImportJobDto>();
            Assert.NotNull(completedJob);

            if (completedJob.Status is EntryImportStatus.Completed or EntryImportStatus.Failed)
            {
                break;
            }
        }

        Assert.NotNull(completedJob);
        Assert.Equal(EntryImportStatus.Completed, completedJob.Status);
        Assert.Equal(3, completedJob.ProcessedRecords);
        Assert.Equal(0, completedJob.FailedRecords);

        // Step 7: Verify imported entries
        HttpResponseMessage getEntriesResponse = await client.GetAsync(
            new Uri($"{Routes.EntryRoutes.GetAll}?habit_id={createdHabit.Id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, getEntriesResponse.StatusCode);

        PaginationResult<EntryDto>? entries = await getEntriesResponse.Content.ReadFromJsonAsync<PaginationResult<EntryDto>>();
        Assert.NotNull(entries);
        Assert.Equal(3, entries.Data.Count);
    }
}
