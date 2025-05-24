namespace DevHabit.Api.Dtos.Common;

public sealed record LinkDto(
    string Href,
    string Rel,
    string Method);
