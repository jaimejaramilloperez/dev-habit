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
        CreateHabitDto dto = HabitsTestData.ValidCreateHabitDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Uri? locationHeader = response.Headers.Location;
        Assert.NotNull(locationHeader);

        HabitDto? habitDto = await response.Content.ReadFromJsonAsync<HabitDto>();

        Assert.NotNull(habitDto);
        Assert.Equal($"/{EndpointRoute}/{habitDto.Id}", locationHeader.AbsolutePath);
    }

    [Fact]
    public async Task CreateHabit_ShouldReturnError_WhenParametersAreInValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateHabitDto dto = HabitsTestData.InValidCreateHabitDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(EndpointRoute, dto);

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
