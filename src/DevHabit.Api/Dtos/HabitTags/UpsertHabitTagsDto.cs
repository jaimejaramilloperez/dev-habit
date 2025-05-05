namespace DevHabit.Api.Dtos.HabitTags;

public sealed record UpsertHabitTagsDto
{
    public required IReadOnlyCollection<string> TagIds { get; init; }
}
