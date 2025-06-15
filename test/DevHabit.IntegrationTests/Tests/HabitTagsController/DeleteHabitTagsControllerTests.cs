using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Dtos.HabitTags;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Entities;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.IntegrationTests.Tests.HabitTagsController;

public sealed class DeleteHabitTagsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task DeleteHabitTag_ShouldSucceed_WhenHabitTagExists()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit
        CreateHabitDto habitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage habitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, habitDto);
        habitResponse.EnsureSuccessStatusCode();

        HabitDto? habit = await habitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(habit);

        // Create a tag
        CreateTagDto tagDto = TagsTestData.ValidCreateTagDto;
        HttpResponseMessage tagResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, tagDto);
        tagResponse.EnsureSuccessStatusCode();

        TagDto? tag = await tagResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(tag);

        // Assign tag to habit
        UpsertHabitTagsDto upsertDto = new() { TagIds = [tag.Id] };
        HttpResponseMessage upsertResponse = await client.PutAsJsonAsync(Routes.HabitTagsRoutes.Upsert(habit.Id), upsertDto);
        upsertResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage response = await client.DeleteAsync(
            new Uri(Routes.HabitTagsRoutes.Delete(habit.Id, tag.Id), UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify tag was unassigned from the habit
        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.HabitRoutes.Get}/{habit.Id}", UriKind.Relative));

        getResponse.EnsureSuccessStatusCode();

        HabitWithTagsDto? result = await getResponse.Content.ReadFromJsonAsync<HabitWithTagsDto>();

        Assert.NotNull(result);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public async Task DeleteHabitTag_ShouldReturnNotFound_WhenHabitTagDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.DeleteAsync(
            new Uri(Routes.HabitTagsRoutes.Delete(Habit.CreateNewId(), Tag.CreateNewId()), UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }
}
