using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Entries.ImportJob;

public static class EntryImportJobMappings
{
    public static EntryImportJob ToEntity(this CreateEntryImportJobDto dto, string userId, byte[] fileContent)
    {
        return new()
        {
            Id = EntryImportJob.CreateNewId(),
            UserId = userId,
            Status = EntryImportStatus.Pending,
            FileName = dto.File.FileName,
            FileContent = fileContent,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public static EntryImportJobDto ToDto(this EntryImportJob entryImportJob)
    {
        return new()
        {
            Id = entryImportJob.Id,
            UserId = entryImportJob.UserId,
            Status = entryImportJob.Status,
            FileName = entryImportJob.FileName,
            CreatedAtUtc = entryImportJob.CreatedAtUtc,
        };
    }
}
