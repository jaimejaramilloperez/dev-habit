using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.HabitsController;

public sealed class PatchHabitsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.HabitRoutes.Patch;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task PatchHabit_ShouldSucceed_WhenParametersAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto createDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        const string newHabitName = "Patched Habit Name";

        JsonPatchDocument<UpdateHabitDto> patchDocument = new();
        patchDocument.Replace(x => x.Name, newHabitName);

        // Act
        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"{EndpointRoute}/{createdHabit.Id}", UriKind.Relative), patchDocument.Operations);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify habit was updated
        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.HabitRoutes.Get}/{createdHabit.Id}", UriKind.Relative));

        HabitWithTagsDto? result = await getResponse.Content.ReadFromJsonAsync<HabitWithTagsDto>();

        Assert.NotNull(result);
        Assert.Equal(newHabitName, result.Name);
    }

    [Fact]
    public async Task PatchHabit_ShouldFail_WhenParametersAreInValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto createDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        UpdateHabitDto updateDto = HabitsTestData.ValidUpdateHabitDto;

        // Act
        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"{EndpointRoute}/{createdHabit.Id}", UriKind.Relative), updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal(2, problem.Errors.Count);
    }

    [Fact]
    public async Task PatchHabit_ShouldReturnNotFound_WhenHabitDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        const string newHabitName = "Patched Habit Name";

        JsonPatchDocument<UpdateHabitDto> patchDocument = new();
        patchDocument.Replace(x => x.Name, newHabitName);

        // Act
        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"{EndpointRoute}/{Habit.CreateNewId()}", UriKind.Relative), patchDocument.Operations);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }
}
