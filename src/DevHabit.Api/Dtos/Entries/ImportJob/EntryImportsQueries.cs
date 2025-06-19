using System.Linq.Expressions;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Entries.ImportJob;

public static class EntryImportQueries
{
    public static Expression<Func<EntryImportJob, EntryImportJobDto>> ProjectToDto()
    {
        return entry => new()
        {
            Id = entry.Id,
            UserId = entry.UserId,
            Status = entry.Status,
            FileName = entry.FileName,
            TotalRecords = entry.TotalRecords,
            ProcessedRecords = entry.ProcessedRecords,
            SuccessfulRecords = entry.SuccessfulRecords,
            FailedRecords = entry.FailedRecords,
            CreatedAtUtc = entry.CreatedAtUtc,
        };
    }
}
