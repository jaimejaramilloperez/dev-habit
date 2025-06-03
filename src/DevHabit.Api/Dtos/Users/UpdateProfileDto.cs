using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.Dtos.Users;

[ValidateNever]
public sealed record UpdateProfileDto
{
    public required string Name { get; set; }
}
