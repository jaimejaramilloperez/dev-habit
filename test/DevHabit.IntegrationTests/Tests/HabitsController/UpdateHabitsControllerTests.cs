using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Habits;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.HabitsController;

public sealed class UpdateHabitsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.HabitRoutes.Update;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task UpdateHabit_ShouldSucceed_WhenParametersAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        CreateHabitDto createDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        UpdateHabitDto updateDto = HabitsTestData.ValidUpdateHabitDto;

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri($"{EndpointRoute}/{createdHabit.Id}", UriKind.Relative),
            updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.HabitRoutes.Get}/{createdHabit.Id}", UriKind.Relative));

        HabitWithTagsDto? updatedHabit = await getResponse.Content.ReadFromJsonAsync<HabitWithTagsDto>();

        Assert.NotNull(updatedHabit);
        Assert.Equal(updateDto.Name, updatedHabit.Name);
        Assert.Equal(updateDto.Description, updatedHabit.Description);
        Assert.Equal(updateDto.Frequency.Type, updatedHabit.Frequency.Type);
        Assert.Equal(updateDto.Frequency.TimesPerPeriod, updatedHabit.Frequency.TimesPerPeriod);
        Assert.Equal(updateDto.Target.Value, updatedHabit.Target.Value);
        Assert.Equal(updateDto.Target.Unit, updatedHabit.Target.Unit);
    }

    [Fact]
    public async Task UpdateHabit_ShouldReturnError_WhenParametersAreInValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        CreateHabitDto createDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        UpdateHabitDto updateDto = HabitsTestData.InValidUpdateHabitDto;

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            new Uri($"{EndpointRoute}/{createdHabit.Id}", UriKind.Relative),
            updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Equal(2, problem.Errors.Count);
    }
}
