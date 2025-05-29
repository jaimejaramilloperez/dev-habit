namespace DevHabit.Api.Configurations;

public sealed class JwtAuthOptions
{
    public required string Key { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int ExpirationInMinutes { get; init; }
    public int RefreshTokenExpirationInDays { get; init; }
}
