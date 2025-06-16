using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.Dtos.Entries;

[ValidateNever]
public sealed record UpdateEntryDto
{
    public required int Value { get; init; }
    public string? Notes { get; init; }
}
