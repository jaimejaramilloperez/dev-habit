using System.Globalization;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Dtos.Tags;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/tags")]
public sealed class TagsController(ApplicationDbContext dbContext)
    : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    [HttpGet]
    public async Task<ActionResult<TagsCollectionDto>> GetTags()
    {
        List<TagDto> tags = await _dbContext.Tags.AsNoTracking()
            .Select(x => x.ToDto())
            .ToListAsync();

        TagsCollectionDto tagsCollectionDto = new()
        {
            Data = tags,
        };

        return Ok(tagsCollectionDto);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTag(string id)
    {
        TagDto? tag = await _dbContext.Tags.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.ToDto())
            .FirstOrDefaultAsync();

        return tag is null ? NotFound() : Ok(tag);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTag(CreateTagDto createTagDto)
    {
        Tag tag = createTagDto.ToEntity();

        bool tagExists = await _dbContext.Tags
            .AnyAsync(x => x.Name.ToLower() == createTagDto.Name.ToLower());

        if (tagExists)
        {
            return Problem(
                title: $"The tag '{createTagDto.Name}' already exists",
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
}
