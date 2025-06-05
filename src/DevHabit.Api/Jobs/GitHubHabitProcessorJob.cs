using DevHabit.Api.Database;
using DevHabit.Api.Dtos.GitHub;
using DevHabit.Api.Entities;
using DevHabit.Api.Services.GitHub;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Jobs;

[DisallowConcurrentExecution]
public sealed class GitHubHabitProcessorJob(
    ApplicationDbContext dbContext,
    GitHubAccessTokenService gitHubAccessTokenService,
    GitHubService gitHubService,
    ILogger<GitHubHabitProcessorJob> logger) : IJob
{
    private const string PushEventType = "PushEvent";

    public async Task Execute(IJobExecutionContext context)
    {
        string habitId = context.JobDetail.JobDataMap.GetString("habitId")
            ?? throw new InvalidOperationException("HabitId not found in job data");

        try
        {
            logger.LogInformation("Processing GitHub events for habit {HabitId}", habitId);

            Habit? habit = await dbContext.Habits.FirstOrDefaultAsync(
                x => x.Id == habitId &&
                x.AutomationSource == AutomationSource.None &&
                !x.IsArchived,
                context.CancellationToken);

            if (habit is null)
            {
                logger.LogWarning("Habit {HabitId} not found or no longer configured for GitHub automation", habitId);
                return;
            }

            string? accessToken = await gitHubAccessTokenService.GetAsync(habit.UserId, context.CancellationToken);

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                logger.LogWarning("No GitHub access token found for user {UserId}", habit.UserId);
                return;
            }

            GitHubUserProfileDto? profile = await gitHubService.GetUserProfileAsync(accessToken, context.CancellationToken);

            if (profile is null)
            {
                logger.LogWarning("Could not retrieve GitHub profile for user {UserId}", habit.UserId);
                return;
            }

            List<GitHubEventDto> gitHubEvents = [];
            const int perPage = 100;
            const int pagesToFetch = 10;

            for (int page = 1; page <= pagesToFetch; page++)
            {
                IReadOnlyList<GitHubEventDto> pageEvents = await gitHubService
                    .GetUserEventsAsync(profile.Login, accessToken, page, perPage, context.CancellationToken);

                if (!pageEvents.Any())
                {
                    break;
                }

                gitHubEvents.AddRange(pageEvents);
            }

            if (!gitHubEvents.Any())
            {
                logger.LogWarning("Could not retrieve GitHub events for user {UserId}", habit.UserId);
                return;
            }

            List<GitHubEventDto> pushEvents = gitHubEvents.Where(x => x.Type == PushEventType).ToList();

            logger.LogInformation("Found {Count} push events for habit {HabitId}", pushEvents.Count, habitId);

            foreach (var pushEvent in pushEvents)
            {
                bool exists = await dbContext.Entries.AnyAsync(
                    x => x.HabitId == habitId &&
                    x.ExternalId == pushEvent.Id,
                    context.CancellationToken);

                if (exists)
                {
                    logger.LogDebug("Entry already exists for event {EventId}", pushEvent.Id);
                    continue;
                }

                Entry entry = new()
                {
                    Id = $"e_{Guid.CreateVersion7()}",
                    HabitId = habitId,
                    UserId = habit.UserId,
                    Value = 1, // each push counts as 1
                    Notes = $"""
                        {pushEvent.Actor.Login} pushed:

                        {string.Join(
                            Environment.NewLine,
                            pushEvent.Payload.Commits?.Select(x => $"- {x.Message}") ?? [])}
                    """,
                    Source = EntrySource.Automation,
                    ExternalId = pushEvent.Id,
                    Date = DateOnly.FromDateTime(pushEvent.CreatedAt.DateTime),
                    CreatedAtUtc = DateTime.UtcNow,
                };

                dbContext.Entries.Add(entry);
                logger.LogInformation("Created entry for event {EventId} on habit {HabitId}", pushEvent.Id, habitId);
            }

            await dbContext.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("Completed processing GitHub events for habit {HabitId}", habitId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing GitHub events for habit {HabitId}", habitId);
            throw;
        }
    }
}
