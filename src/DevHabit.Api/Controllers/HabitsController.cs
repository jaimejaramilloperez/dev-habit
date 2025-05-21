using System.Net.Mime;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Services;
using DevHabit.Api.Services.DataShapingServices;
using DevHabit.Api.Services.LinkServices;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/habits")]
public sealed class HabitsController(
    ApplicationDbContext dbContext,
    ILinkService linkService) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILinkService _linkService = linkService;

    [HttpGet]
    [Produces(MediaTypeNames.Application.Json, CustomMediaTypes.Application.HateoasJson)]
    public async Task<IActionResult> GetHabits(
        HabitsParameters habitParams,
        IDataShapingService dataShapingService,
        IValidator<HabitsParameters> validator)
    {
        await validator.ValidateAndThrowAsync(habitParams);

        string? searchTerm = habitParams.SearchTerm?.Trim().ToLowerInvariant();

        bool shouldIncludeLinks = string.Equals(
            habitParams.Accept,
            CustomMediaTypes.Application.HateoasJson,
            StringComparison.OrdinalIgnoreCase);

        ShapedPaginationResult paginationResult = await _dbContext.Habits.AsNoTracking()
            .Where(x =>
                searchTerm == null ||
                x.Name.ToLower().Contains(searchTerm) ||
                x.Description != null && x.Description.ToLower().Contains(searchTerm))
            .Where(x => habitParams.Type == null || x.Type == habitParams.Type)
            .Where(x => habitParams.Status == null || x.Status == habitParams.Status)
            .SortByQueryString(habitParams.Sort, HabitMappings.SortMapping.Mappings)
            .Select(HabitQueries.ProjectToDto())
            .ToShapedPaginationAsync(new()
            {
                Page = habitParams.Page,
                PageSize = habitParams.PageSize,
                Fields = habitParams.Fields,
                DataShaping = dataShapingService,
                LinksFactory = shouldIncludeLinks ? x => CreateLinksForHabit(x.Id, habitParams.Fields) : null,
            });

        if (shouldIncludeLinks)
        {
            paginationResult.Links.AddRange(CreateLinksForHabits(
                habitParams,
                paginationResult.HasPreviousPage,
                paginationResult.HasNextPage));
        }

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    [Produces(MediaTypeNames.Application.Json, CustomMediaTypes.Application.HateoasJson)]
    public async Task<IActionResult> GetHabit(
        string id,
        HabitParameters habitParameters,
        IDataShapingService dataShapingService)
    {
        ShapedResult? result = await _dbContext.Habits.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(HabitQueries.ProjectToDtoWithTags())
            .ToShapedFirstOrDefaultAsync(dataShapingService, habitParameters.Fields);

        if (result is null)
        {
            return NotFound();
        }

        var shapedHabitDto = result.Item;

        bool shouldIncludeLinks = string.Equals(
            habitParameters.Accept,
            CustomMediaTypes.Application.HateoasJson,
            StringComparison.OrdinalIgnoreCase);

        if (shouldIncludeLinks)
        {
            shapedHabitDto.TryAdd("Links", CreateLinksForHabit(id, habitParameters.Fields));
        }

        return Ok(shapedHabitDto);
    }

    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        IValidator<CreateHabitDto> validator)
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

        Habit habit = createHabitDto.ToEntity();

        _dbContext.Habits.Add(habit);

        await _dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto(CreateLinksForHabit(habit.Id));

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

    private List<LinkDto> CreateLinksForHabit(string id, string? fields = null) =>
    [
        _linkService.Create(nameof(GetHabit), "self", HttpMethods.Get, new { id, fields }),
        _linkService.Create(nameof(UpdateHabit), "update", HttpMethods.Put, new { id }),
        _linkService.Create(nameof(PatchHabit), "patch", HttpMethods.Patch, new { id }),
        _linkService.Create(nameof(DeleteHabit), "delete", HttpMethods.Delete, new { id }),
        _linkService.Create(
            endpointName: nameof(HabitTagsController.UpsertHabitTags),
            rel: "upsert-tags",
            method: HttpMethods.Put,
            values: new { habitId = id },
            controllerName: HabitTagsController.Name),
    ];

    private List<LinkDto> CreateLinksForHabits(
        HabitsParameters parameters,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        List<LinkDto> links =
        [
            _linkService.Create(nameof(GetHabits), "self", HttpMethods.Get, new
            {
                q = parameters.SearchTerm,
                type = parameters.Type,
                status = parameters.Status,
                fields = parameters.Fields,
                sort = parameters.Sort,
                page = parameters.Page,
                page_size = parameters.PageSize,
            }),
            _linkService.Create(nameof(CreateHabit), "create", HttpMethods.Post),
        ];

        if (hasPreviousPage)
        {
            links.Add(_linkService.Create(nameof(GetHabits), "previous-page", HttpMethods.Get, new
            {
                q = parameters.SearchTerm,
                type = parameters.Type,
                status = parameters.Status,
                fields = parameters.Fields,
                sort = parameters.Sort,
                page = parameters.Page - 1,
                page_size = parameters.PageSize,
            }));
        }

        if (hasNextPage)
        {
            links.Add(_linkService.Create(nameof(GetHabits), "next-page", HttpMethods.Get, new
            {
                q = parameters.SearchTerm,
                type = parameters.Type,
                status = parameters.Status,
                fields = parameters.Fields,
                sort = parameters.Sort,
                page = parameters.Page + 1,
                page_size = parameters.PageSize,
            }));
        }

        return links;
    }
}
