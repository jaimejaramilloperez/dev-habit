using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Entries.ImportJob;

public sealed record EntryImportJobDto
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required EntryImportStatus Status { get; init; }
    public required string FileName { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}

