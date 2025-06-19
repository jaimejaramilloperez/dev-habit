using System.Net;
using System.Net.Http.Json;
using DevHabit.Api.Dtos.Auth;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Dtos.HabitTags;
using DevHabit.Api.Dtos.Tags;
using DevHabit.FunctionalTests.Infrastructure;
using DevHabit.FunctionalTests.TestData;

namespace DevHabit.FunctionalTests.Tests;

public sealed class HabitManagementFlowTests(DevHabitWebAppFactory appFactory)
    : FunctionalTestFixture(appFactory), IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Fact]
    public async Task CompleteHabitManagementFlow_ShouldSucceed()
    {
        // Arrange
        const string email = "habitflow@test.com";
        const string password = "Test123!";

        HttpClient client = CreateClient();

        // Act

        // Step 1: Register a new user
        RegisterUserDto registerUserDto = new()
        {
            Name = email,
            Email = email,
            Password = password,
            ConfirmationPassword = password,
        };

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Register, registerUserDto);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        // Step 2: Login to get the token
        LoginUserDto loginUserDto = new()
        {
            Email = email,
            Password = password,
        };

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(Routes.AuthRoutes.Login, loginUserDto);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        AccessTokensDto? accessTokens = await loginResponse.Content.ReadFromJsonAsync<AccessTokensDto>();
        Assert.NotNull(accessTokens);

        client.DefaultRequestHeaders.Authorization = new("Bearer", accessTokens.AccessToken);

        // Step 3: Create a habit
        CreateHabitDto createHabitDto = HabitsTestData.CreateReadingHabitDto();
        HttpResponseMessage createHabitResponse = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, createHabitDto);
        Assert.Equal(HttpStatusCode.Created, createHabitResponse.StatusCode);

        HabitDto? createdHabit = await createHabitResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(createdHabit);
        Assert.Equal(createHabitDto.Name, createdHabit.Name);

        // Step 4: Create a tag
        CreateTagDto createTagDto = TagsTestData.CreateTagDto();
        HttpResponseMessage createTagResponse = await client.PostAsJsonAsync(Routes.TagRoutes.Create, createTagDto);
        Assert.Equal(HttpStatusCode.Created, createTagResponse.StatusCode);

        TagDto? createdTag = await createTagResponse.Content.ReadFromJsonAsync<TagDto>();
        Assert.NotNull(createdTag);
        Assert.Equal(createTagDto.Name, createdTag.Name);

        // Step 5: Assign the tag to the habit
        UpsertHabitTagsDto upsertHabitTagsDto = HabitTagsTestData.CreateUpsertDto([createdTag.Id]);

        HttpResponseMessage upsertResponse = await client.PutAsJsonAsync(
            Routes.HabitTagsRoutes.Upsert(createdHabit.Id), upsertHabitTagsDto);

        Assert.Equal(HttpStatusCode.NoContent, upsertResponse.StatusCode);

        // Step 6: Get habit and verify the tag
        HttpResponseMessage habitsResponse = await client.GetAsync(
            new Uri($"{Routes.HabitRoutes.Get}/{createdHabit.Id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, habitsResponse.StatusCode);

        HabitWithTagsDto? habitWithTags = await habitsResponse.Content.ReadFromJsonAsync<HabitWithTagsDto>();
        Assert.NotNull(habitWithTags);
        Assert.Equal(createdTag.Name, habitWithTags.Tags.First());
    }
}
