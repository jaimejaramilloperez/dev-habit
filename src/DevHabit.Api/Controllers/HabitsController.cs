using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/habits")]
public sealed class HabitsController(ApplicationDbContext dbContext) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    [HttpGet]
    public async Task<IActionResult> GetHabits(
        HabitsQueryParameters queryParams,
        IDataShapingService dataShapingService,
        IValidator<HabitsQueryParameters> validator)
    {
        await validator.ValidateAndThrowAsync(queryParams);

        string? searchTerm = queryParams.SearchTerm?.Trim().ToLowerInvariant();

        ShapedPaginationResult shapedPaginationResult = await _dbContext.Habits.AsNoTracking()
            .Where(x =>
                searchTerm == null ||
                x.Name.ToLower().Contains(searchTerm) ||
                x.Description != null && x.Description.ToLower().Contains(searchTerm))
            .Where(x => queryParams.Type == null || x.Type == queryParams.Type)
            .Where(x => queryParams.Status == null || x.Status == queryParams.Status)
            .SortByQueryString(queryParams.Sort, HabitMappings.SortMapping.Mappings)
            .Select(HabitQueries.ProjectToDto())
            .ToShapedPaginationResult(new()
            {
                Page = queryParams.Page,
                PageSize = queryParams.PageSize,
                Fields = queryParams.Fields,
                DataShaping = dataShapingService,
            });

        return Ok(shapedPaginationResult);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHabit(
        string id,
        string? fields,
        IDataShapingService dataShapingService)
    {
        ShapedResult? result = await _dbContext.Habits.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(HabitQueries.ProjectToDtoWithTags())
            .FirstOrDefaultAsyncShapedResult(dataShapingService, fields);

        return result is null
            ? NotFound()
            : Ok(result.Item);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator)
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

        Habit habit = createHabitDto.ToEntity();

        _dbContext.Habits.Add(habit);
        await _dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHabit(string id, UpdateHabitDto updateHabitDto)
    {
        Habit? habit = await _dbContext.Habits.FirstOrDefaultAsync(x => x.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        habit.UpdateFromDto(updateHabitDto);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchHabit(string id, JsonPatchDocument<HabitDto> patchDocument)
    {
        Habit? habit = await _dbContext.Habits.FirstOrDefaultAsync(x => x.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        HabitDto habitDto = habit.ToDto();

        patchDocument.ApplyTo(habitDto, ModelState);

        if (!TryValidateModel(habitDto))
        {
            return ValidationProblem(ModelState);
        }

        habit.Name = habitDto.Name;
        habit.Description = habitDto.Description;
        habit.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHabit(string id)
    {
        Habit? habit = await _dbContext.Habits.FirstOrDefaultAsync(x => x.Id == id);

        if (habit is null)
        {
            return NotFound();
        }

        _dbContext.Habits.Remove(habit);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
