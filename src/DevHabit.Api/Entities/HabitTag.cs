namespace DevHabit.Api.Entities;

public sealed class HabitTag
{
    public required string HabitId { get; set; }
    public required string TagId { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public Habit Habit { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
