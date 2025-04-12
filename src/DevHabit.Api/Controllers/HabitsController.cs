using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Habits;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/habits")]
public sealed class HabitsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public HabitsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<HabitsCollectionDto>> GetHabits()
    {
        List<HabitDto> habits = await _dbContext.Habits.Select(x => new HabitDto(
                x.Id,
                x.Name,
                x.Description,
                x.Type,
                new FrequencyDto(x.Frequency.Type, x.Frequency.TimesPerPeriod),
                new TargetDto(x.Target.Value, x.Target.Unit),
                x.Status,
                x.IsArchived,
                x.EndDate,
                x.Milestone == null ? null : new MilestoneDto(x.Milestone.Target, x.Milestone.Current),
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.LastCompletedAtUtc))
            .AsNoTracking()
            .ToListAsync();

        HabitsCollectionDto habitsCollectionDto = new(habits);

        return Ok(habitsCollectionDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HabitDto>> GetHabit(string id)
    {
        HabitDto? habit = await _dbContext.Habits.Where(x => x.Id == id)
            .Select(x => new HabitDto(
                x.Id,
                x.Name,
                x.Description,
                x.Type,
                new FrequencyDto(x.Frequency.Type, x.Frequency.TimesPerPeriod),
                new TargetDto(x.Target.Value, x.Target.Unit),
                x.Status,
                x.IsArchived,
                x.EndDate,
                x.Milestone == null ? null : new MilestoneDto(x.Milestone.Target, x.Milestone.Current),
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.LastCompletedAtUtc))
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return habit is null ? NotFound() : Ok(habit);
    }
}
