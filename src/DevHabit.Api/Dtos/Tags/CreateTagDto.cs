using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.Dtos.Tags;

[ValidateNever]
public sealed record CreateTagDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
