using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Habits;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.HabitsController;

public sealed class CreateHabitsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.HabitRoutes.Create;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task CreateHabit_ShouldSucceed_WhenParametersAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateHabitDto createDto = HabitsTestData.ValidCreateHabitDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, createDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.NotNull(locationHeader);

        HabitDto? result = await response.Content.ReadFromJsonAsync<HabitDto>();

        Assert.NotNull(result);
        Assert.Equal($"/{EndpointRoute}/{result.Id}", locationHeader.AbsolutePath);
        Assert.NotEmpty(result.Id);
    }

    [Fact]
    public async Task CreateHabit_ShouldReturnError_WhenParametersAreInValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateHabitDto createDto = HabitsTestData.InValidCreateHabitDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, createDto);

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
}
