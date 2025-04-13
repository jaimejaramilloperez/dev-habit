namespace DevHabit.Api.Dtos.HabitTags;

public sealed record UpsertHabitTagsDto
{
    public required ICollection<string> TagIds { get; init; }
}
