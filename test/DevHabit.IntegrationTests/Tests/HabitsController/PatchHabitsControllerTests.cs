using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Habits;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;

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
            new Uri($"{EndpointRoute}/{createdHabit.Id}", UriKind.Relative),
            patchDocument.Operations);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.HabitRoutes.Get}/{createdHabit.Id}", UriKind.Relative));

        HabitWithTagsDto? patchedHabit = await getResponse.Content.ReadFromJsonAsync<HabitWithTagsDto>();

        Assert.NotNull(patchedHabit);
        Assert.Equal(newHabitName, patchedHabit.Name);
    }
}
