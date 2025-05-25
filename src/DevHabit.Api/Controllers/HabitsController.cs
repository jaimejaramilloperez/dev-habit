using System.Net.Mime;
using DevHabit.Api.Common;
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
    [Produces(MediaTypeNames.Application.Json, CustomMediaTypesNames.Application.HateoasJson)]
    public async Task<IActionResult> GetHabits(
        HabitsParameters habitParams,
        IValidator<HabitsParameters> validator)
    {
        await validator.ValidateAndThrowAsync(habitParams);

        string? searchTerm = habitParams.SearchTerm?.Trim().ToLowerInvariant();

        bool shouldIncludeLinks = HateoasHelpers.ShouldIncludeHateoas(habitParams.Accept);

        ShapedPaginationResult paginationResult = await _dbContext.Habits.AsNoTracking()
            .Where(x =>
                searchTerm == null ||
                x.Name.ToLower().Contains(searchTerm) ||
                x.Description != null && x.Description.ToLower().Contains(searchTerm))
            .Where(x => habitParams.Type == null || x.Type == habitParams.Type)
            .Where(x => habitParams.Status == null || x.Status == habitParams.Status)
            .SortByQueryString(habitParams.Sort, HabitMappings.SortMapping.Mappings)
            .Select(HabitQueries.ProjectToDto())
            .ToShapedPaginationResultAsync(new()
            {
                Page = habitParams.Page,
                PageSize = habitParams.PageSize,
                Fields = habitParams.Fields,
                HttpContext = HttpContext,
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
    public async Task<IActionResult> GetHabit(string id, HabitParameters habitParameters)
    {
        string? fields = habitParameters.Fields;

        ShapedResult? result = await _dbContext.Habits.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(HabitQueries.ProjectToDtoWithTags())
            .ToShapedFirstOrDefaultAsync(new()
            {
                Fields = fields,
                Links = CreateLinksForHabit(id, fields),
                HttpContext = HttpContext,
            });

        return result is null ? NotFound() : Ok(result.Item);
    }

    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        [FromHeader(Name = "Accept")] string? acceptHeader,
        IDataShapingService dataShapingService,
        IValidator<CreateHabitDto> validator)
    {
        await validator.ValidateAndThrowAsync(createHabitDto);

        Habit habit = createHabitDto.ToEntity();

        _dbContext.Habits.Add(habit);

        await _dbContext.SaveChangesAsync();

        HabitDto habitDto = habit.ToDto();

        if (HateoasHelpers.ShouldIncludeHateoas(acceptHeader))
        {
            var result = dataShapingService.ShapeData(habitDto, null, CreateLinksForHabit(habitDto.Id));
            return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, result);
        }

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

    private ICollection<LinkDto> CreateLinksForHabit(string id, string? fields = null) =>
    [
        _linkService.Create(nameof(GetHabit), LinkRelations.Self, HttpMethods.Get, new { id, fields }),
        _linkService.Create(nameof(UpdateHabit), LinkRelations.Update, HttpMethods.Put, new { id }),
        _linkService.Create(nameof(PatchHabit), LinkRelations.Patch, HttpMethods.Patch, new { id }),
        _linkService.Create(nameof(DeleteHabit), LinkRelations.Delete, HttpMethods.Delete, new { id }),
        _linkService.Create(
            endpointName: nameof(HabitTagsController.UpsertHabitTags),
            rel: LinkRelations.UpsertTags,
            method: HttpMethods.Put,
            values: new { habitId = id },
            controllerName: HabitTagsController.Name),
    ];

    private ICollection<LinkDto> CreateLinksForHabits(
        HabitsParameters parameters,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        ICollection<LinkDto> links =
        [
            _linkService.Create(nameof(GetHabits), LinkRelations.Self, HttpMethods.Get, new
            {
                q = parameters.SearchTerm,
                type = parameters.Type,
                status = parameters.Status,
                fields = parameters.Fields,
                sort = parameters.Sort,
                page = parameters.Page,
                page_size = parameters.PageSize,
            }),
            _linkService.Create(nameof(CreateHabit), LinkRelations.Create, HttpMethods.Post),
        ];

        if (hasPreviousPage)
        {
            links.Add(_linkService.Create(nameof(GetHabits), LinkRelations.PreviousPage, HttpMethods.Get, new
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
            links.Add(_linkService.Create(nameof(GetHabits), LinkRelations.NextPage, HttpMethods.Get, new
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
