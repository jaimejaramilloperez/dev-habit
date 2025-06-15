using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Dtos.Habits;
using DevHabit.IntegrationTests.Infrastructure;
using DevHabit.IntegrationTests.TestData;
using Microsoft.AspNetCore.Http;

namespace DevHabit.IntegrationTests.Tests;

public sealed class CrossCuttingTests(DevHabitWebAppFactory appFactory)
    : IntegrationTestFixture(appFactory), IAsyncLifetime
{
    public static TheoryData<string> ProtectedEndpoints =>
    [
        Routes.HabitRoutes.GetAll,
        Routes.TagRoutes.GetAll,
        Routes.UserRoutes.CurrentUser,
        Routes.GitHubRoutes.GetUserProfile,
        Routes.GitHubRoutes.GetUserEvents,
    ];

    public static TheoryData<string> MediaTypes =>
    [
        MediaTypeNames.Application.Json,
        CustomMediaTypeNames.Application.HateoasJson,
    ];

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await CleanUpDatabaseAsync();

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Endpoints_ShouldRequireAuthentication(string endpoint)
    {
        // Arrange
        HttpClient client = CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(endpoint, UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.First().Scheme);
    }

    [Fact]
    public async Task Endpoints_ShouldEnforceResourceOwnership()
    {
        // Arrange
        HttpClient client1 = await CreateAuthenticatedClientAsync(email: "user1@example.com", name: "user1", forceNewClient: true);
        HttpClient client2 = await CreateAuthenticatedClientAsync(email: "user2@example.com", name: "user2", forceNewClient: true);

        // Create a habit with user1
        CreateHabitDto habitDto = HabitsTestData.ValidCreateHabitDto;
        HttpResponseMessage createResponse = await client1.PostAsJsonAsync(Routes.HabitRoutes.Create, habitDto);
        createResponse.EnsureSuccessStatusCode();

        HabitDto? habit = await createResponse.Content.ReadFromJsonAsync<HabitDto>();
        Assert.NotNull(habit);

        // Act
        HttpResponseMessage response = await client2.GetAsync(
            new Uri($"{Routes.HabitRoutes.Get}/{habit.Id}", UriKind.Relative));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(MediaTypes))]
    public async Task Api_ShouldSupportContentNegotiation(string mediaType)
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new(mediaType));

        // Act
        HttpResponseMessage response = await client.GetAsync(new Uri(Routes.HabitRoutes.GetAll, UriKind.Relative));

        // Assert
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Api_ShouldReturnProblemDetails_WhenAnErrorOccurs()
    {
        // Arrange
        HttpClient client = await CreateAuthenticatedClientAsync();
        CreateHabitDto dto = HabitsTestData.InValidCreateHabitDto;

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(Routes.HabitRoutes.Create, dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.ProblemJson, response.Content.Headers.ContentType?.MediaType);

        Dictionary<string, object>? problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        Assert.NotNull(problem);
        Assert.True(problem.ContainsKey("type"));
        Assert.True(problem.ContainsKey("title"));
        Assert.True(problem.ContainsKey("status"));
        Assert.True(problem.ContainsKey("detail"));
        Assert.True(problem.ContainsKey("errors"));
        Assert.True(problem.ContainsKey("traceId"));
        Assert.True(problem.ContainsKey("requestId"));
    }
}
