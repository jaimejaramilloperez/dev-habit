namespace DevHabit.Api.Dtos.Entries;

public sealed record CreateEntryDto
{
    public required string HabitId { get; init; }
    public required int Value { get; init; }
    public string? Notes { get; init; }
    public required DateOnly Date { get; init; }
}
