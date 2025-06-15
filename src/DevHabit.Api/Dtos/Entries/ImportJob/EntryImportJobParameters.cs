using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Dtos.Entries.ImportJob;

public sealed record EntryImportJobParameters : AcceptHeaderDto
{
    public string? Fields { get; init; }
}
