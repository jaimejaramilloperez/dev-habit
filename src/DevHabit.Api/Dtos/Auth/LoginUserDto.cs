using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.Dtos.Auth;

[ValidateNever]
public sealed record LoginUserDto
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
