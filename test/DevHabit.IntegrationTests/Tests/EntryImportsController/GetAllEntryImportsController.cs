using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using DevHabit.Api.Common.Pagination;
using DevHabit.Api.Dtos.Entries.ImportJob;
using DevHabit.Api.Dtos.Habits;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;

namespace DevHabit.IntegrationTests.Tests.EntryImportsController;

public sealed class GetAllEntryImportsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.EntryImportJobRoutes.GetAll;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetImportJobs_ShouldReturnEmptyList_WhenNoImportJobsExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<EntryImportJobDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<EntryImportJobDto>>();

        Assert.NotNull(result);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetImportJobs_ShouldReturnImportJobs_WhenImportJobsExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto habitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage habitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, habitDto);
        habitResponse.EnsureSuccessStatusCode();

        HabitDto? habit = await habitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(habit);

        string csvContent = $"""
            habit_id,date,notes
            {habit.Id},2025-01-01,Started the year strong
            {habit.Id},2025-01-02,Making progress
            {habit.Id},2025-01-03,Getting better
        """;

        using MultipartFormDataContent content = [];
        using ByteArrayContent file = new(Encoding.UTF8.GetBytes(csvContent));
        file.Headers.ContentType = new(MediaTypeNames.Text.Csv);
        content.Add(file, "file", "entries.csv");

        HttpResponseMessage createResponse = await client.PostAsync(
            new Uri(Routes.EntryImportJobRoutes.Create, UriKind.Relative), content);

        createResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(EndpointRoute, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<EntryImportJobDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<EntryImportJobDto>>();

        Assert.NotNull(result);
        Assert.Single(result.Data);
    }

    [Fact]
    public async Task GetImportJobs_ShouldSupportDataShaping_WhenFieldsAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto habitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage habitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, habitDto);
        habitResponse.EnsureSuccessStatusCode();

        HabitDto? habit = await habitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(habit);

        string csvContent = $"""
            habit_id,date,notes
            {habit.Id},2025-01-01,Started the year strong
            {habit.Id},2025-01-02,Making progress
            {habit.Id},2025-01-03,Getting better
        """;

        using MultipartFormDataContent content = [];
        using ByteArrayContent file = new(Encoding.UTF8.GetBytes(csvContent));
        file.Headers.ContentType = new(MediaTypeNames.Text.Csv);
        content.Add(file, "file", "entries.csv");

        HttpResponseMessage createResponse = await client.PostAsync(
            new Uri(Routes.EntryImportJobRoutes.Create, UriKind.Relative), content);

        createResponse.EnsureSuccessStatusCode();

        EntryImportJobDto? createdJob = await createResponse.Content.ReadFromJsonAsync<EntryImportJobDto>();
        Assert.NotNull(createdJob);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?fields=id,fileName,status", UriKind.Relative));

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
        Assert.True(item.ContainsKey("fileName"));
        Assert.True(item.ContainsKey("status"));
        Assert.False(item.ContainsKey("userId"));
        Assert.False(item.ContainsKey("createdAtUtc"));
    }

    [Fact]
    public async Task GetImportJobs_ShouldSupportPagination_WhenPaginationParametersAreProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto habitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage habitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, habitDto);
        habitResponse.EnsureSuccessStatusCode();

        HabitDto? habit = await habitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(habit);

        // Create import jobs
        string csvContent = $"""
            habit_id,date,notes
            {habit.Id},2025-01-01,Started the year strong
            {habit.Id},2025-01-02,Making progress
            {habit.Id},2025-01-03,Getting better
        """;

        for (int i = 0; i < 3; i++)
        {
            using MultipartFormDataContent content = [];
            using ByteArrayContent file = new(Encoding.UTF8.GetBytes(csvContent));
            file.Headers.ContentType = new(MediaTypeNames.Text.Csv);
            content.Add(file, "file", "entries.csv");

            HttpResponseMessage createResponse = await client.PostAsync(
                new Uri(Routes.EntryImportJobRoutes.Create, UriKind.Relative), content);

            createResponse.EnsureSuccessStatusCode();
        }

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}?page=1&page_size=2", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaginationResult<EntryImportJobDto>? result = await response.Content.ReadFromJsonAsync<PaginationResult<EntryImportJobDto>>();

        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
    }
}
