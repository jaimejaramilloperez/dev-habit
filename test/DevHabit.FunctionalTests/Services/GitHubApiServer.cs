using System.Globalization;
using System.Net.Mime;
using System.Text.Json;
using DevHabit.Api.Dtos.GitHub;
using Microsoft.AspNetCore.Http;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace DevHabit.FunctionalTests.Services;

public sealed class GitHubApiServer : IDisposable
{
    private WireMockServer? _server;
    private bool _disposed;

    public string Url => _server?.Url
        ?? throw new InvalidOperationException("Server not started");

    private static JsonSerializerOptions SerializerOptions => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _server ??= WireMockServer.Start();
    }

    public void Stop()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _server?.Stop();
        _server = null;
    }

    public void SetUpValidUser()
    {
        if (_server is null)
        {
            throw new InvalidOperationException("Server not started");
        }

        var request = Request.Create()
            .WithPath("/user")
            .WithHeader("Authorization", $"Bearer {GitHubConstants.GitHubAccessToken}")
            .UsingGet();

        GitHubUserProfileDto userProfile = GenerateGitHubUserProfile(GitHubConstants.GitHubUser);
        string userResponse = JsonSerializer.Serialize(userProfile, SerializerOptions);

        var response = Response.Create()
            .WithBody(userResponse)
            .WithHeader("Content-Type", MediaTypeNames.Application.Json)
            .WithStatusCode(StatusCodes.Status200OK);

        _server.Given(request).RespondWith(response);
    }

    public void SetUpValidEvent()
    {
        if (_server is null)
        {
            throw new InvalidOperationException("Server not started");
        }

        var request = Request.Create()
            .WithPath($"/users/{GitHubConstants.GitHubUser}/events")
            .WithHeader("Authorization", $"Bearer {GitHubConstants.GitHubAccessToken}")
            .UsingGet();

        GitHubEventDto userEvent = GenerateGitHubUserEvent(GitHubConstants.GitHubUser);
        List<GitHubEventDto> events = [userEvent];
        string userResponse = JsonSerializer.Serialize(events, SerializerOptions);

        var response = Response.Create()
            .WithBody(userResponse)
            .WithHeader("Content-Type", MediaTypeNames.Application.Json)
            .WithStatusCode(StatusCodes.Status200OK);

        _server.Given(request).RespondWith(response);
    }

    private static GitHubUserProfileDto GenerateGitHubUserProfile(string username) => new(
        Login: username,
        Id: 1,
        NodeId: "MDQ6VXNl1jE=",
        AvatarUrl: new("https://avatars.githubusercontent.com/u/1?v=4"),
        GravatarId: "",
        Url: new($"https://api.github.com/users/{username}"),
        HtmlUrl: new($"https://github.com/{username}"),
        FollowersUrl: new($"https://api.github.com/users/{username}/followers"),
        FollowingUrl: new($"https://api.github.com/users/{username}/following{{/other_user}}"),
        GistsUrl: new($"https://api.github.com/users/{username}/gists{{/gist_id}}"),
        StarredUrl: new($"https://api.github.com/users/{username}/starred{{/owner}}{{/repo}}"),
        SubscriptionsUrl: new($"https://api.github.com/users/{username}/subscriptions"),
        OrganizationsUrl: new($"https://api.github.com/users/{username}/orgs"),
        ReposUrl: new($"https://api.github.com/users/{username}/repos"),
        EventsUrl: new($"https://api.github.com/users/{username}/events{{/privacy}}"),
        ReceivedEventsUrl: new($"https://api.github.com/users/{username}/received_events"),
        Type: "User",
        SiteAdmin: false,
        Name: username,
        Company: "Test Company",
        Blog: new($"https://{username}.dev"),
        Location: "Test City",
        Email: $"{username}@example.com",
        Hireable: true,
        Bio: $"{username} bio",
        TwitterUsername: username,
        PublicRepos: 10,
        PublicGists: 2,
        Followers: 100,
        Following: 50,
        CreatedAt: DateTime.Parse("2020-01-01T00:00:00Z", CultureInfo.InvariantCulture),
        UpdatedAt: DateTime.Parse("2025-01-01T00:00:00Z", CultureInfo.InvariantCulture),
        PrivateGists: 1,
        TotalPrivateRepos: 3,
        OwnedPrivateRepos: 2,
        DiskUsage: 2048,
        Collaborators: 5,
        TwoFactorAuthentication: true,
        Plan: new(
            Name: "pro",
            Space: 976562499,
            PrivateRepos: 9999,
            Collaborators: 0
        )
    );

    private static GitHubEventDto GenerateGitHubUserEvent(string username) => new(
        Id: "1234567890",
        Type: "PushEvent",
        Actor: new(
            Id: 1001,
            Login: username,
            DisplayLogin: username,
            GravatarId: "",
            Url: new($"https://api.github.com/users/{username}"),
            AvatarUrl: new("https://avatars.githubusercontent.com/u/1?v=4")
        ),
        Repo: new(
            Id: 2001,
            Name: $"{username}/TestRepo",
            Url: new($"https://api.github.com/repos/{username}/TestRepo")
        ),
        Payload: new(
            Action: "pushed",
            Commits:
            [
                new(
                    Sha: "abcdef1234567890",
                    Author: new(Email: $"{username}@example.com", Name: username),
                    Message: "Initial commit",
                    Distinct: true,
                    Url: new($"https://github.com/{username}/TestRepo/commit/abcdef1234567890")
                )
            ]
        ),
        Public: true,
        CreatedAt: DateTime.Parse("2025-01-01T00:00:00Z", CultureInfo.InvariantCulture)
    );

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _server?.Stop();
                _server?.Dispose();
                _server = null;
            }

            _disposed = true;
        }
    }
}
