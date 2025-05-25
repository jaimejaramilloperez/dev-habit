using DevHabit.Api.Common;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Services.LinkServices;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/tags")]
public sealed class TagsController(
    ApplicationDbContext dbContext,
    ILinkService linkService) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILinkService _linkService = linkService;

    [HttpGet]
    public async Task<IActionResult> GetTags(
        TagsParameters tagsParameters,
        IValidator<TagsParameters> validator)
    {
        await validator.ValidateAndThrowAsync(tagsParameters);

        string? searchTerm = tagsParameters.SearchTerm?.Trim().ToLowerInvariant();

        PaginationResult<TagDto> result = await _dbContext.Tags.AsNoTracking()
            .Where(x =>
                searchTerm == null ||
                x.Name.ToLower().Contains(searchTerm) ||
                x.Description != null && x.Description.ToLower().Contains(searchTerm))
            .SortByQueryString(tagsParameters.Sort, TagMappings.SortMapping.Mappings)
            .Select(TagQueries.ProjectToDto())
            .ToPaginationResultAsync(tagsParameters.Page, tagsParameters.PageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTag(string id, TagParameters tagParameters)
    {
        string? fields = tagParameters.Fields;

        ShapedResult? result = await _dbContext.Tags.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(TagQueries.ProjectToDto())
            .ToShapedFirstOrDefaultAsync(new()
            {
                Fields = fields,
                Links = CreateLinksForTag(id, fields),
                HttpContext = HttpContext,
            });

        return result is null ? NotFound() : Ok(result.Item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTag(
        CreateTagDto createTagDto,
        IValidator<CreateTagDto> validator)
    {
        await validator.ValidateAndThrowAsync(createTagDto);

        Tag tag = createTagDto.ToEntity();

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

        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTag(string id, UpdateTagDto updateTagDto)
    {
        Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id);

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
        Tag? tag = await _dbContext.Tags.FirstOrDefaultAsync(x => x.Id == id);

        if (tag is null)
        {
            return NotFound();
        }

        _dbContext.Tags.Remove(tag);

        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private ICollection<LinkDto> CreateLinksForTag(string id, string? fields) =>
    [
        _linkService.Create(nameof(GetTag), LinkRelations.Self, HttpMethods.Get, new { id, fields }),
        _linkService.Create(nameof(UpdateTag), LinkRelations.Update, HttpMethods.Put, new { id }),
        _linkService.Create(nameof(DeleteTag), LinkRelations.Delete, HttpMethods.Delete, new { id }),
    ];
}
