namespace DevHabit.Api.Dtos.Auth;

public sealed record TokenRequestDto(
    string UserId,
    string Email,
    IEnumerable<string> Roles);
