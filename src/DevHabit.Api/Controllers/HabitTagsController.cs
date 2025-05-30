using DevHabit.Api.Database;
using DevHabit.Api.Dtos.HabitTags;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("/api/habits/{habitId}/tags")]
[Authorize]
public sealed class HabitTagsController(
    ApplicationDbContext dbContext,
    UserContext userContext) : ControllerBase
{
    public static readonly string Name = nameof(HabitTagsController)
        .Replace("Controller", string.Empty, StringComparison.Ordinal);

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly UserContext _userContext = userContext;

    [HttpPut]
    public async Task<IActionResult> UpsertHabitTags(string habitId, UpsertHabitTagsDto upsertHabitTagsDto)
    {
        string? userId = await _userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Habit? habit = await _dbContext.Habits
            .Include(x => x.HabitTags)
            .FirstOrDefaultAsync(x => x.Id == habitId && x.UserId == userId);

        if (habit is null)
        {
            return NotFound();
        }

        HashSet<string> currentTagIds = habit.HabitTags.Select(x => x.TagId).ToHashSet();

        if (currentTagIds.SetEquals(upsertHabitTagsDto.TagIds))
        {
            return NoContent();
        }

        List<string> existingTagIds = await _dbContext.Tags
            .Where(x => upsertHabitTagsDto.TagIds.Contains(x.Id) && x.UserId == userId)
            .Select(x => x.Id)
            .ToListAsync();

        if (existingTagIds.Count != upsertHabitTagsDto.TagIds.Count)
        {
            return Problem(
                detail: "One or more tag IDs are invalid",
                statusCode: StatusCodes.Status400BadRequest);
        }

        habit.HabitTags.RemoveAll(x => !upsertHabitTagsDto.TagIds.Contains(x.TagId));

        string[] tagIdsToAdd = upsertHabitTagsDto.TagIds.Except(currentTagIds).ToArray();

        habit.HabitTags.AddRange(tagIdsToAdd.Select(tagId => new HabitTag
        {
            HabitId = habitId,
            TagId = tagId,
            CreatedAtUtc = DateTime.UtcNow,
        }));

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{tagId}")]
    public async Task<IActionResult> DeleteHabitTag(string habitId, string tagId)
    {
        string? userId = await _userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        HabitTag? habitTag = await _dbContext.HabitTags
            .Join(_dbContext.Habits,
                ht => ht.HabitId,
                h => h.Id,
                (ht, h) => new { HabitTag = ht, Habit = h })
            .Join(_dbContext.Tags,
                hth => hth.HabitTag.TagId,
                t => t.Id,
                (hth, t) => new { hth.HabitTag, hth.Habit, Tag = t })
            .Where(x => x.HabitTag.HabitId == habitId
                    && x.HabitTag.TagId == tagId
                    && x.Habit.UserId == userId
                    && x.Tag.UserId == userId)
            .Select(x => x.HabitTag)
            .FirstOrDefaultAsync();

        if (habitTag is null)
        {
            return NotFound();
        }

        _dbContext.HabitTags.Remove(habitTag);

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
