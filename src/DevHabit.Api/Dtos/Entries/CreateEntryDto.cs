using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.Dtos.Entries;

[ValidateNever]
public sealed record CreateEntryDto
{
    public required string HabitId { get; init; }
    public required int Value { get; init; }
    public string? Notes { get; init; }
    public required DateOnly Date { get; init; }
}
