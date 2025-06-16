using System.Net.Mime;
using DevHabit.Api.Common.Auth;
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
[Authorize(Roles = Roles.Member)]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public sealed class HabitTagsController(
    ApplicationDbContext dbContext,
    UserContext userContext) : ControllerBase
{
    public static readonly string Name = nameof(HabitTagsController)
        .Replace("Controller", string.Empty, StringComparison.Ordinal);

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly UserContext _userContext = userContext;

    [HttpPut]
    [EndpointSummary("Update habit tags")]
    [EndpointDescription("Updates the tags associated with a habit by replacing the existing tag collection with the provided one.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertHabitTags(
        string habitId,
        UpsertHabitTagsDto upsertHabitTagsDto,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Habit? habit = await _dbContext.Habits
            .Include(x => x.HabitTags)
            .FirstOrDefaultAsync(x => x.Id == habitId && x.UserId == userId, cancellationToken);

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
            .ToListAsync(cancellationToken);

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

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{tagId}")]
    [EndpointSummary("Remove a tag from a habit")]
    [EndpointDescription("Removes the association between a specific tag and habit, identified by their respective IDs.")]
    public async Task<IActionResult> DeleteHabitTag(
        string habitId,
        string tagId,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        HabitTag? habitTag = await _dbContext.HabitTags
            .Where(x =>
                x.HabitId == habitId &&
                x.TagId == tagId &&
                x.Habit.UserId == userId &&
                x.Tag.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (habitTag is null)
        {
            return NotFound();
        }

        _dbContext.HabitTags.Remove(habitTag);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
