using Asp.Versioning;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/habits")]
[Authorize(Roles = Roles.Member)]
[ApiVersion(1.0)]
[Produces(
    CustomMediaTypeNames.Application.JsonV1,
    CustomMediaTypeNames.Application.JsonV2,
    CustomMediaTypeNames.Application.HateoasJson,
    CustomMediaTypeNames.Application.HateoasJsonV1,
    CustomMediaTypeNames.Application.HateoasJsonV2)]
public sealed class HabitsController(
    ApplicationDbContext dbContext,
    UserContext userContext,
    LinkService linkService) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly UserContext _userContext = userContext;
    private readonly LinkService _linkService = linkService;

    [HttpGet]
    public async Task<IActionResult> GetHabits(
        HabitsParameters habitParameters,
        IValidator<HabitsParameters> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(habitParameters, cancellationToken);

        var (searchTerm, type, status, sort, fields, page, pageSize) = habitParameters;

        string? normalizedSearchTerm = searchTerm?.Trim().ToLowerInvariant();

        ShapedPaginationResult<HabitDto> paginationResult = await _dbContext.Habits.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x =>
                normalizedSearchTerm == null ||
                x.Name.ToLower().Contains(normalizedSearchTerm) ||
                x.Description != null && x.Description.ToLower().Contains(normalizedSearchTerm))
            .Where(x => type == null || x.Type == type)
            .Where(x => status == null || x.Status == status)
            .SortByQueryString(sort, HabitMappings.SortMapping.Mappings)
            .Select(HabitQueries.ProjectToDto())
            .ToShapedPaginationResultAsync(page, pageSize, fields, cancellationToken)
            .WithHateoasAsync(new()
            {
                ItemLinksFactory = x => CreateLinksForHabit(x.Id, fields),
                CollectionLinksFactory = x => CreateLinksForHabits(habitParameters, x.HasPreviousPage, x.HasNextPage),
                AcceptHeader = habitParameters.Accept,
            }, cancellationToken);

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    [MapToApiVersion(1.0)]
    public async Task<IActionResult> GetHabit(
        string id,
        HabitParameters habitParameters,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        string? fields = habitParameters.Fields;

        ShapedResult<HabitWithTagsDto>? result = await _dbContext.Habits.AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(HabitQueries.ProjectToDtoWithTags())
            .ToShapedFirstOrDefaultAsync(fields, cancellationToken)
            .WithHateoasAsync(CreateLinksForHabit(id, fields), habitParameters.Accept, cancellationToken);

        return result is null ? NotFound() : Ok(result.Item);
    }

    [HttpGet("{id}")]
    [ApiVersion(2.0)]
    public async Task<IActionResult> GetHabitV2(
        string id,
        HabitParameters habitParameters,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        string? fields = habitParameters.Fields;

        ShapedResult<HabitWithTagsDtoV2>? result = await _dbContext.Habits.AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(HabitQueries.ProjectToDtoWithTagsV2())
            .ToShapedFirstOrDefaultAsync(fields, cancellationToken)
            .WithHateoasAsync(CreateLinksForHabit(id, fields), habitParameters.Accept, cancellationToken);

        return result is null ? NotFound() : Ok(result.Item);
    }

    [HttpPost]
    public async Task<ActionResult<HabitDto>> CreateHabit(
        CreateHabitDto createHabitDto,
        AcceptHeaderDto acceptHeaderDto,
        IValidator<CreateHabitDto> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createHabitDto, cancellationToken);

        Habit habit = createHabitDto.ToEntity(userId);

        _dbContext.Habits.Add(habit);

        await _dbContext.SaveChangesAsync(cancellationToken);

        HabitDto habitDto = habit.ToDto();

        if (HateoasHelpers.ShouldIncludeHateoas(acceptHeaderDto.Accept))
        {
            var result = DataShaper.ShapeData(habitDto, CreateLinksForHabit(habitDto.Id));
            return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, result);
        }

        return CreatedAtAction(nameof(GetHabit), new { id = habitDto.Id }, habitDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHabit(
        string id,
        UpdateHabitDto updateHabitDto,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Habit? habit = await _dbContext.Habits
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (habit is null)
        {
            return NotFound();
        }

        habit.UpdateFromDto(updateHabitDto);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchHabit(
        string id,
        JsonPatchDocument<HabitDto> patchDocument,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Habit? habit = await _dbContext.Habits
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

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

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHabit(string id, CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Habit? habit = await _dbContext.Habits
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (habit is null)
        {
            return NotFound();
        }

        _dbContext.Habits.Remove(habit);

        await _dbContext.SaveChangesAsync(cancellationToken);

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
