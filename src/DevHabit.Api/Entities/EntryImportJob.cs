namespace DevHabit.Api.Entities;

public sealed class EntryImportJob
{
    public required string Id { get; set; }
    public required string UserId { get; set; }
    public required EntryImportStatus Status { get; set; }
    public required string FileName { get; set; }
    public required ICollection<byte> FileContent { get; init; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int SuccessfulRecords { get; set; }
    public int FailedRecords { get; set; }
    public ICollection<string> Errors { get; init; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public enum EntryImportStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
}
