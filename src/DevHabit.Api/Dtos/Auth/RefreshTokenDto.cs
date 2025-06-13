using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DevHabit.Api.Dtos.Auth;

[ValidateNever]
public sealed record RefreshTokenDto(string RefreshToken);
