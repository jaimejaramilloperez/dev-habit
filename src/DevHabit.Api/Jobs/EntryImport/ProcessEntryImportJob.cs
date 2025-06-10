using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using DevHabit.Api.Database;
using DevHabit.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IO;
using Quartz;

namespace DevHabit.Api.Jobs.EntryImport;

public sealed class ProcessEntryImportJob(
    ApplicationDbContext dbContext,
    RecyclableMemoryStreamManager streamManager,
    ILogger<ProcessEntryImportJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        string importJobId = context.MergedJobDataMap.GetString("importJobId")!;

        EntryImportJob? importJob = await dbContext.EntryImportJobs
            .FirstOrDefaultAsync(x => x.Id == importJobId, context.CancellationToken);

        if (importJob is null)
        {
            logger.LogError("Import job {ImportJobId} not found", importJobId);
            return;
        }

        try
        {
            importJob.Status = EntryImportStatus.Processing;
            await dbContext.SaveChangesAsync(context.CancellationToken);

            using RecyclableMemoryStream memoryStream = streamManager.GetStream(importJob.FileContent.ToArray());
            using StreamReader streamReader = new(memoryStream);
            using CsvReader csv = new(streamReader, CultureInfo.InvariantCulture);

            List<CsvEntryRecord> records = csv.GetRecords<CsvEntryRecord>().ToList();
            importJob.TotalRecords = records.Count;
            await dbContext.SaveChangesAsync(context.CancellationToken);

            foreach (var record in records)
            {
                try
                {
                    Habit? habit = await dbContext.Habits
                        .FirstOrDefaultAsync(x => x.Id == record.HabitId && x.UserId == importJob.UserId, context.CancellationToken);

                    if (habit is null)
                    {
                        throw new InvalidOperationException($"Habit with ID '{record.HabitId}' does not exists or does not belong to the user");
                    }

                    Entry entry = new()
                    {
                        Id = Entry.CreateNewId(),
                        HabitId = record.HabitId,
                        UserId = importJob.UserId,
                        Value = habit.Target.Value,
                        Notes = record.Notes,
                        Source = EntrySource.FileImport,
                        Date = record.Date,
                        CreatedAtUtc = DateTime.UtcNow,
                    };

                    dbContext.Entries.Add(entry);
                    importJob.SuccessfulRecords++;
                }
                catch (Exception ex)
                {
                    bool isSafeException = ex is InvalidOperationException;

                    string errorMessage = isSafeException
                        ? $"Error processing record: {ex.Message}"
                        : $"Error processing record";

                    importJob.FailedRecords++;
                    importJob.Errors.Add(errorMessage);

                    if (importJob.Errors.Count >= 100)
                    {
                        importJob.Errors.Add("Too many errors, stopping error collection...");
                        break;
                    }
                }
                finally
                {
                    importJob.ProcessedRecords++;
                }

                if (importJob.ProcessedRecords % 100 == 0)
                {
                    await dbContext.SaveChangesAsync(context.CancellationToken);
                }
            }

            importJob.Status = EntryImportStatus.Completed;
            importJob.CompletedAtUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing import job {ImportJobId}", importJobId);

            importJob.Status = EntryImportStatus.Failed;
            importJob.CompletedAtUtc = DateTime.UtcNow;
            importJob.Errors.Add("Fatal error");
        }
        finally
        {
            await dbContext.SaveChangesAsync(context.CancellationToken);
        }
    }
}

public sealed record CsvEntryRecord
{
    [Name("habit_id")]
    public required string HabitId { get; init; }

    [Name("date")]
    public required DateOnly Date { get; init; }

    [Name("notes")]
    public string? Notes { get; init; }
}
