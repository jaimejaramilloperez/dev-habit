using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Dtos.Habits;

public sealed record HabitParameters : AcceptHeaderDto
{
    public string? Fields { get; init; }
}
