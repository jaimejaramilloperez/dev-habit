namespace DevHabit.Api.Entities;

public sealed class GitHubAccessToken
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string Token { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    public static string CreateNewId() => $"gh_{Guid.CreateVersion7()}";
}
