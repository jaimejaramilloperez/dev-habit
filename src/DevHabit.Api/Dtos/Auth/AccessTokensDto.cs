namespace DevHabit.Api.Dtos.Auth;

public sealed record AccessTokensDto(
    string AccessToken,
    string RefreshToken);
