using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Dtos.Entries;

public sealed record EntryParameters : AcceptHeaderDto
{
    public string? Fields { get; init; }
}
