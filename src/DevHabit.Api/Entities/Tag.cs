namespace DevHabit.Api.Entities;

public sealed class Tag
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
