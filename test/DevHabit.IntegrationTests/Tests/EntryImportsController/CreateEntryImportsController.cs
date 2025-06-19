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

public sealed class CreateEntryImportsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.EntryImportJobRoutes.Create;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task CreateImportJob_ShouldSucceed_WhenAValidFileIsProvided()
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

        // Act
        HttpResponseMessage response = await client.PostAsync(new Uri(EndpointRoute, UriKind.Relative), content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.NotNull(locationHeader);

        EntryImportJobDto? result = await response.Content.ReadFromJsonAsync<EntryImportJobDto>();

        Assert.NotNull(result);
        Assert.Equal($"/{EndpointRoute}/{result.Id}", locationHeader.AbsolutePath);
        Assert.NotEmpty(result.Id);
        Assert.Equal(EntryImportStatus.Pending, result.Status);
    }

    [Fact]
    public async Task CreateImportJob_ShouldFail_WhenAnInValidFileIsProvided()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        using MultipartFormDataContent content = [];
        using ByteArrayContent file = new("Invalid Content"u8.ToArray());
        file.Headers.ContentType = new(MediaTypeNames.Text.Plain);
        content.Add(file, "file", "entries.txt");

        // Act
        HttpResponseMessage response = await client.PostAsync(new Uri(EndpointRoute, UriKind.Relative), content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.Null(locationHeader);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Single(problem.Errors);
    }
}
