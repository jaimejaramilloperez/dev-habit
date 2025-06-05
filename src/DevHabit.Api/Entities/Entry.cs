namespace DevHabit.Api.Entities;

public sealed class Entry
{
    public required string Id { get; set; }
    public required string HabitId { get; set; }
    public required string UserId { get; set; }
    public int Value { get; set; }
    public string? Notes { get; set; }
    public EntrySource Source { get; init; }
    public string? ExternalId { get; init; }
    public bool IsArchived { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Habit Habit { get; set; } = null!;
}

public enum EntrySource
{
    Manual = 0,
    Automation = 1,
}
