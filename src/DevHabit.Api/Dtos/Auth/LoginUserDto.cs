using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.Dtos.Auth;

[ValidateNever]
public sealed record LoginUserDto(
    string Email,
    string Password);
