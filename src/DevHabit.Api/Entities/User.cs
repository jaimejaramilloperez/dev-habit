namespace DevHabit.Api.Entities;

public sealed class User
{
    public required string Id { get; set; }
    public string? IdentityId { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public static string CreateNewId() => $"u_{Guid.CreateVersion7()}";
}
