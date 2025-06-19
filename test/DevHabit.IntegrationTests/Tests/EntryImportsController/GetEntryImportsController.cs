using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using DevHabit.Api.Dtos.Entries.ImportJob;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.EntryImportsController;

public sealed class GetEntryImportsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.EntryImportJobRoutes.Get;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetImportJob_ShouldReturnImportJob_WhenImportJobsExists()
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
            new Uri($"{EndpointRoute}/{createdJob.Id}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        EntryImportJobDto? result = await response.Content.ReadFromJsonAsync<EntryImportJobDto>();

        Assert.NotNull(result);
        Assert.Equal(createdJob.Id, result.Id);
    }

    [Fact]
    public async Task GetImportJob_ShouldReturnNotFound_WhenNoImportJobsExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}/{EntryImportJob.CreateNewId()}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }

    [Fact]
    public async Task GetImportJob_ShouldSupportDataShaping_WhenFieldsAreValid()
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
            new Uri($"{EndpointRoute}/{createdJob.Id}?fields=id,fileName,status", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ExpandoObject? result = await response.Content.ReadFromJsonAsync<ExpandoObject>();
        Assert.NotNull(result);

        IDictionary<string, object?> item = result;

        Assert.Equal(3, item.Count);
        Assert.True(item.ContainsKey("id"));
        Assert.True(item.ContainsKey("fileName"));
        Assert.True(item.ContainsKey("status"));
        Assert.False(item.ContainsKey("userId"));
        Assert.False(item.ContainsKey("createdAtUtc"));
    }
}
