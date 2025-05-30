using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public sealed class TagsController(
    ApplicationDbContext dbContext,
    LinkService linkService,
    UserContext userContext) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly LinkService _linkService = linkService;
    private readonly UserContext _userContext = userContext;

    [HttpGet]
    public async Task<IActionResult> GetTags(
        TagsParameters tagsParameters,
        IValidator<TagsParameters> validator)
    {
        string? userId = await _userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(tagsParameters);

        string? searchTerm = tagsParameters.SearchTerm?.Trim().ToLowerInvariant();

        ShapedPaginationResult<TagDto> result = await _dbContext.Tags.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x =>
                searchTerm == null ||
                x.Name.ToLower().Contains(searchTerm) ||
                x.Description != null && x.Description.ToLower().Contains(searchTerm))
            .SortByQueryString(tagsParameters.Sort, TagMappings.SortMapping.Mappings)
            .Select(TagQueries.ProjectToDto())
            .ToShapedPaginationResultAsync(tagsParameters.Page, tagsParameters.PageSize, tagsParameters.Fields)
            .WithHateoasAsync(new()
            {
                ItemLinksFactory = x => CreateLinksForTag(x.Id, tagsParameters.Fields),
                CollectionLinksFactory = x => CreateLinksForTags(tagsParameters, x.HasPreviousPage, x.HasNextPage),
                AcceptHeader = tagsParameters.Accept,
            });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTag(string id, TagParameters tagParameters)
    {
        string? userId = await _userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        string? fields = tagParameters.Fields;

        ShapedResult? result = await _dbContext.Tags.AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(TagQueries.ProjectToDto())
            .ToShapedFirstOrDefaultAsync(fields)
            .WithHateoasAsync(CreateLinksForTag(id, fields), tagParameters.Accept);

        return result is null ? NotFound() : Ok(result.Item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTag(
        CreateTagDto createTagDto,
        AcceptHeaderDto acceptHeaderDto,
        IValidator<CreateTagDto> validator)
    {
        string? userId = await _userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createTagDto);

        Tag tag = createTagDto.ToEntity(userId);

        bool tagExists = await _dbContext.Tags
            .AnyAsync(x => x.Name.ToLower() == createTagDto.Name.ToLower());

        if (tagExists)
        {
            return Problem(
                detail: $"The tag '{createTagDto.Name}' already exists",
                statusCode: StatusCodes.Status409Conflict);
        }

        _dbContext.Tags.Add(tag);

        await _dbContext.SaveChangesAsync();

        TagDto tagDto = tag.ToDto();

        if (HateoasHelpers.ShouldIncludeHateoas(acceptHeaderDto.Accept))
        {
            var result = DataShaper.ShapeData(tagDto, CreateLinksForTag(tagDto.Id));
            return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, result);
        }

        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTag(string id, UpdateTagDto updateTagDto)
    {
        string? userId = await _userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (tag is null)
        {
            return NotFound();
        }

        bool tagWithNameExists = await _dbContext.Tags
            .AnyAsync(x => x.Id != id && x.Name.ToLower() == updateTagDto.Name.ToLower());

        if (tagWithNameExists)
        {
            return Problem(
                title: $"The tag '{updateTagDto.Name}' already exists",
                statusCode: StatusCodes.Status409Conflict);
        }

        tag.UpdateFromDto(updateTagDto);

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTag(string id)
    {
        string? userId = await _userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (tag is null)
        {
            return NotFound();
        }

        _dbContext.Tags.Remove(tag);

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private ICollection<LinkDto> CreateLinksForTag(string id, string? fields = null) =>
    [
        _linkService.Create(nameof(GetTag), LinkRelations.Self, HttpMethods.Get, new { id, fields }),
        _linkService.Create(nameof(UpdateTag), LinkRelations.Update, HttpMethods.Put, new { id }),
        _linkService.Create(nameof(DeleteTag), LinkRelations.Delete, HttpMethods.Delete, new { id }),
    ];

    private ICollection<LinkDto> CreateLinksForTags(
        TagsParameters parameters,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        ICollection<LinkDto> links =
        [
            _linkService.Create(nameof(GetTags), LinkRelations.Self, HttpMethods.Get, new
            {
                q = parameters.SearchTerm,
                fields = parameters.Fields,
                sort = parameters.Sort,
                page = parameters.Page,
                page_size = parameters.PageSize,
            }),
            _linkService.Create(nameof(CreateTag), LinkRelations.Create, HttpMethods.Post),
        ];

        if (hasPreviousPage)
        {
            links.Add(_linkService.Create(nameof(GetTags), LinkRelations.PreviousPage, HttpMethods.Get, new
            {
                q = parameters.SearchTerm,
                fields = parameters.Fields,
                sort = parameters.Sort,
                page = parameters.Page - 1,
                page_size = parameters.PageSize,
            }));
        }

        if (hasNextPage)
        {
            links.Add(_linkService.Create(nameof(GetTags), LinkRelations.NextPage, HttpMethods.Get, new
            {
                q = parameters.SearchTerm,
                fields = parameters.Fields,
                sort = parameters.Sort,
                page = parameters.Page + 1,
                page_size = parameters.PageSize,
            }));
        }

        return links;
    }
}
