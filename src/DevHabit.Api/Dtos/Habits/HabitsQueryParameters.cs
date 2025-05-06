using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Habits;

public sealed record HabitsQueryParameters
{
    [FromQuery(Name = "q")]
    public string? SearchTerm { get; init; }

    public HabitType? Type { get; init; }

    public HabitStatus? Status { get; init; }
    public string? Sort { get; init; }
}
