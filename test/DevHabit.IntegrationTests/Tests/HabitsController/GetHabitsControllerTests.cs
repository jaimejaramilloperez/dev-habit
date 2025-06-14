using System.Dynamic;
using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.HabitsController;

public sealed class GetHabitsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    private const string EndpointRoute = Routes.HabitRoutes.Get;

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task GetHabit_ShouldReturnHabit_WhenHabitExists()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto createDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}/{createdHabit.Id}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        HabitWithTagsDto? result = await response.Content.ReadFromJsonAsync<HabitWithTagsDto>();

        Assert.NotNull(result);
        Assert.Equal(createdHabit.Id, result.Id);
    }

    [Fact]
    public async Task GetHabit_ShouldReturnNotFound_WhenHabitDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}/{Habit.CreateNewId()}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }

    [Fact]
    public async Task GetHabit_ShouldSupportVersioning()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto createDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Add("Accept", CustomMediaTypeNames.Application.HateoasJsonV2);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}/{createdHabit.Id}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(CustomMediaTypeNames.Application.HateoasJsonV2, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetHabit_ShouldSupportDataShaping_WhenFieldsAreValid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit first
        CreateHabitDto createDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createDto);
        createResponse.EnsureSuccessStatusCode();

        HabitDto? createdHabit = await createResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            new Uri($"{EndpointRoute}/{createdHabit.Id}?fields=id,name,tags", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ExpandoObject? result = await response.Content.ReadFromJsonAsync<ExpandoObject>();
        Assert.NotNull(result);

        IDictionary<string, object?> item = result;

        Assert.Equal(3, item.Count);
        Assert.True(item.ContainsKey("id"));
        Assert.True(item.ContainsKey("name"));
        Assert.True(item.ContainsKey("tags"));
        Assert.False(item.ContainsKey("description"));
        Assert.False(item.ContainsKey("status"));
        Assert.False(item.ContainsKey("createdAtUtc"));
    }
}
