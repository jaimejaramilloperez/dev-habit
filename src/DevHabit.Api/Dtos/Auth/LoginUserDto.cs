namespace DevHabit.Api.Dtos.Auth;

public sealed record LoginUserDto(
    string Email,
    string Password);
