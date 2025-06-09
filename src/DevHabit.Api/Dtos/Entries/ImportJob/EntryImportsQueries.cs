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
            CreatedAtUtc = entry.CreatedAtUtc,
        };
    }
}
