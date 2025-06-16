using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Dtos.Habits;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.EntriesController;

public sealed class CreateBatchEntriesControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.EntryRoutes.CreateBatch;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task CreateEntry_ShouldSucceed_WhenParametersAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto createHabitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createdResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        createdResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createdResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        CreateEntryBatchDto createBatchDto = EntriesTestData.GetValidCreateEntryBatchDto(createdHabit.Id);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, createBatchDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.NotNull(locationHeader);

        EntryDto[]? result = await response.Content.ReadFromJsonAsync<EntryDto[]>();

        Assert.NotNull(result);
        Assert.Equal($"/{Routes.EntryRoutes.GetAll}", locationHeader.AbsolutePath);
        Assert.NotEmpty(result);
        Assert.Equal(2, result.Length);
    }

    [Fact]
    public async Task CreateEntry_ShouldReturnError_WhenParametersAreInValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto createHabitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createdResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        createdResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createdResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        CreateEntryBatchDto createBatchDto = EntriesTestData.InvalidCreateEntryBatchDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, createBatchDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.Null(locationHeader);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Equal(2, problem.Errors.Count);
    }

    [Fact]
    public async Task CreateEntry_ShouldReturnError_WhenOneHabitDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateEntryBatchDto createBatchDto = EntriesTestData.GetValidCreateEntryBatchDto();

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, createBatchDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.Null(locationHeader);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Equal("One or more habit IDs are invalid", problem.Detail);
        Assert.Empty(problem.Errors);
    }
}
