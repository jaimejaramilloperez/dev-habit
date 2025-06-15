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

public sealed class UpsertHabitTagsControllerTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task UpsertHabitTags_ShouldSucceed_WhenParametersAreValid()
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

        UpsertHabitTagsDto upsertDto = new() { TagIds = [tag.Id] };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(habit.Id), upsertDto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify tag was assigned to the habit
        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.HabitRoutes.Get}/{habit.Id}", UriKind.Relative));

        getResponse.EnsureSuccessStatusCode();

        HabitWithTagsDto? result = await getResponse.Content.ReadFromJsonAsync<HabitWithTagsDto>();

        Assert.NotNull(result);
        Assert.Single(result.Tags);
        Assert.Equal(tag.Name, result.Tags.First());
    }

    [Fact]
    public async Task UpsertHabitTags_ShouldSucceed_WhenReplacingExistingTags()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit
        CreateHabitDto habitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage habitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, habitDto);
        habitResponse.EnsureSuccessStatusCode();

        HabitDto? habit = await habitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(habit);

        // Create two tags
        List<TagDto> tagDtos = [];

        for (int i = 1; i <= 2; i++)
        {
            CreateTagDto createTagDto = TagsTestData.ValidCreateTagDto with { Name = $"Tag #{i}" };
            HttpResponseMessage tagResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, createTagDto);
            tagResponse.EnsureSuccessStatusCode();

            TagDto? tagDto = await tagResponse.Content.ReadFromJsonAsync<TagDto>();
            Assert.NotNull(tagDto);

            tagDtos.Add(tagDto);
        }

        // Assign first tag to habit
        UpsertHabitTagsDto firstUpsertDto = new() { TagIds = [tagDtos[0].Id] };

        HttpResponseMessage firstUpsertResponse = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(habit.Id), firstUpsertDto);

        firstUpsertResponse.EnsureSuccessStatusCode();

        // Now replace with second tag
        UpsertHabitTagsDto secondUpsertDto = new() { TagIds = [tagDtos[1].Id] };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(habit.Id), secondUpsertDto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify second tag was assigned to the habit
        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.HabitRoutes.Get}/{habit.Id}", UriKind.Relative));

        getResponse.EnsureSuccessStatusCode();

        HabitWithTagsDto? result = await getResponse.Content.ReadFromJsonAsync<HabitWithTagsDto>();

        Assert.NotNull(result);
        Assert.Single(result.Tags);
        Assert.Equal(tagDtos[1].Name, result.Tags.First());
    }

    [Fact]
    public async Task UpsertHabitTags_ShouldSucceed_WhenAddingAndReplacingExistingTags()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit
        CreateHabitDto habitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage habitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, habitDto);
        habitResponse.EnsureSuccessStatusCode();

        HabitDto? habit = await habitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(habit);

        // Create four tags
        List<TagDto> tagDtos = [];

        for (int i = 1; i <= 4; i++)
        {
            CreateTagDto createTagDto = TagsTestData.ValidCreateTagDto with { Name = $"Tag #{i}" };
            HttpResponseMessage tagResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, createTagDto);
            tagResponse.EnsureSuccessStatusCode();

            TagDto? tagDto = await tagResponse.Content.ReadFromJsonAsync<TagDto>();
            Assert.NotNull(tagDto);

            tagDtos.Add(tagDto);
        }

        // Assign first and second tag to habit
        UpsertHabitTagsDto firstUpsertDto = new() { TagIds = [tagDtos[0].Id, tagDtos[1].Id] };

        HttpResponseMessage firstUpsertResponse = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(habit.Id), firstUpsertDto);

        firstUpsertResponse.EnsureSuccessStatusCode();

        // Now replace second tag with third tag and add fourth tag
        UpsertHabitTagsDto secondUpsertDto = new() { TagIds = [tagDtos[0].Id, tagDtos[2].Id, tagDtos[3].Id] };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(habit.Id), secondUpsertDto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify habit tags
        HttpResponseMessage getResponse = await client.GetAsync(
            new Uri($"{Routes.HabitRoutes.Get}/{habit.Id}", UriKind.Relative));

        getResponse.EnsureSuccessStatusCode();

        HabitWithTagsDto? result = await getResponse.Content.ReadFromJsonAsync<HabitWithTagsDto>();

        Assert.NotNull(result);
        Assert.Equal(3, result.Tags.Count);
        Assert.Equal(tagDtos[0].Name, result.Tags.ElementAt(0));
        Assert.Equal(tagDtos[2].Name, result.Tags.ElementAt(1));
        Assert.Equal(tagDtos[3].Name, result.Tags.ElementAt(2));
    }

    [Fact]
    public async Task UpsertHabitTags_ShouldReturnNoContent_WhenTagsAreUnchanged()
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
        UpsertHabitTagsDto firstUpsertDto = new() { TagIds = [tag.Id] };

        HttpResponseMessage firstUpsertResponse = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(habit.Id), firstUpsertDto);

        firstUpsertResponse.EnsureSuccessStatusCode();

        // Try to upsert with the same tag
        UpsertHabitTagsDto secondUpsertDto = new() { TagIds = [tag.Id] };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(habit.Id), secondUpsertDto);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpsertHabitTags_ShouldReturnNotFound_WhenHabitDoesNotExist()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a tag
        CreateTagDto tagDto = TagsTestData.ValidCreateTagDto;
        HttpResponseMessage tagResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, tagDto);
        tagResponse.EnsureSuccessStatusCode();

        TagDto? tag = await tagResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(tag);

        UpsertHabitTagsDto upsertDto = new() { TagIds = [tag.Id] };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(Habit.CreateNewId()), upsertDto);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Not Found", problem.Title);
        Assert.Empty(problem.Errors);
    }

    [Fact]
    public async Task UpsertHabitTags_ShouldReturnBadRequest_WhenTagIdsAreInvalid()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();

        // Create a habit
        CreateHabitDto habitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage habitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, habitDto);
        habitResponse.EnsureSuccessStatusCode();

        HabitDto? habit = await habitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(habit);

        UpsertHabitTagsDto upsertDto = new() { TagIds = [Tag.CreateNewId()] };

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(habit.Id), upsertDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Bad Request", problem.Title);
        Assert.Equal("One or more tag IDs are invalid", problem.Detail);
    }
}
