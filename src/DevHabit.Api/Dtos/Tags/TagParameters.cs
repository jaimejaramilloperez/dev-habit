using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Dtos.Tags;

public sealed record TagParameters : AcceptHeaderDto
{
    public string? Fields { get; init; }
}
