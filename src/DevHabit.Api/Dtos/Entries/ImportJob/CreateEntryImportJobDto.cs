namespace DevHabit.Api.Dtos.Entries.ImportJob;

public sealed record CreateEntryImportJobDto
{
    public required IFormFile File { get; init; }
}
