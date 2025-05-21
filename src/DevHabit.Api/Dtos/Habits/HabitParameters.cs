using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Habits;

public sealed record HabitParameters
{
    public string? Fields { get; init; }

    [FromHeader(Name = "Accept")]
    public string? Accept { get; set; }
}
