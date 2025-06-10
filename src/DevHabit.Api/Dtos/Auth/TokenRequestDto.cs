namespace DevHabit.Api.Dtos.Auth;

public sealed record TokenRequestDto
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required IEnumerable<string> Roles { get; init; }
}
