using System.Net.Mime;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Common.Pagination;
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
[Authorize(Roles = Roles.Member)]
[ResponseCache(Duration = 120, VaryByHeader = "Accept")]
[Produces(MediaTypeNames.Application.Json, CustomMediaTypeNames.Application.HateoasJson)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class TagsController(
    ApplicationDbContext dbContext,
    UserContext userContext,
    LinkService linkService) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly UserContext _userContext = userContext;
    private readonly LinkService _linkService = linkService;

    [HttpGet]
    [EndpointSummary("Get all tags")]
    [EndpointDescription("Retrieves a paginated list of tags with optional filtering, sorting, and field selection.")]
    [ProducesResponseType<PaginationResult<TagDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTags(
        TagsParameters tagsParameters,
        IValidator<TagsParameters> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(tagsParameters, cancellationToken);

        var (searchTerm, sort, fields, page, pageSize) = tagsParameters;

        string? normalizedSearchTerm = searchTerm?.Trim().ToLowerInvariant();

        ShapedPaginationResult<TagDto> result = await _dbContext.Tags.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x =>
                normalizedSearchTerm == null ||
                x.Name.ToLower().Contains(normalizedSearchTerm) ||
                x.Description != null && x.Description.ToLower().Contains(normalizedSearchTerm))
            .SortByQueryString(sort, TagMappings.SortMapping.Mappings)
            .Select(TagQueries.ProjectToDto())
            .ToShapedPaginationResultAsync(page, pageSize, fields, cancellationToken)
            .WithHateoasAsync(new()
            {
                ItemLinksFactory = x => CreateLinksForTag(x.Id, fields),
                CollectionLinksFactory = x => CreateLinksForTags(tagsParameters, x.HasPreviousPage, x.HasNextPage, x.Data.Count),
                AcceptHeader = tagsParameters.Accept,
            }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get a tag by ID")]
    [EndpointDescription("Retrieves a specific tag by its unique identifier with optional field selection.")]
    [ProducesResponseType<TagDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTag(
        string id,
        TagParameters tagParameters,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        string? fields = tagParameters.Fields;

        ShapedResult<TagDto>? result = await _dbContext.Tags.AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(TagQueries.ProjectToDto())
            .ToShapedFirstOrDefaultAsync(fields, cancellationToken)
            .WithHateoasAsync(CreateLinksForTag(id, fields), tagParameters.Accept, cancellationToken);

        return result is null ? NotFound() : Ok(result.Item);
    }

    [HttpPost]
    [EndpointSummary("Create a new tag")]
    [EndpointDescription("Creates a new tag with the provided details. Tag names must be unique per user.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<TagDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTag(
        CreateTagDto createTagDto,
        AcceptHeaderDto acceptHeaderDto,
        IValidator<CreateTagDto> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createTagDto, cancellationToken);

        Tag tag = createTagDto.ToEntity(userId);

        bool tagExists = await _dbContext.Tags
            .AnyAsync(x =>
                x.Name.ToLower() == createTagDto.Name.ToLower() &&
                x.UserId == userId,
            cancellationToken);

        if (tagExists)
        {
            return Problem(
                detail: $"The tag '{createTagDto.Name}' already exists",
                statusCode: StatusCodes.Status409Conflict);
        }

        _dbContext.Tags.Add(tag);

        await _dbContext.SaveChangesAsync(cancellationToken);

        TagDto tagDto = tag.ToDto();

        if (HateoasHelpers.ShouldIncludeHateoas(acceptHeaderDto.Accept))
        {
            var result = DataShaper.ShapeData(tagDto, CreateLinksForTag(tagDto.Id));
            return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, result);
        }

        return CreatedAtAction(nameof(GetTag), new { id = tagDto.Id }, tagDto);
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update a tag")]
    [EndpointDescription("Updates an existing tag's details. Tag names must remain unique per user.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTag(
        string id,
        UpdateTagDto updateTagDto,
        IValidator<UpdateTagDto> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(updateTagDto, cancellationToken);

        Tag? tag = await _dbContext.Tags
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (tag is null)
        {
            return NotFound();
        }

        bool tagWithNameExists = await _dbContext.Tags
            .AnyAsync(x =>
                x.Id != id &&
                x.Name.ToLower() == updateTagDto.Name.ToLower()
                && x.UserId == userId,
            cancellationToken);

        if (tagWithNameExists)
        {
            return Problem(
                detail: $"The tag '{updateTagDto.Name}' already exists",
                statusCode: StatusCodes.Status409Conflict);
        }

        tag.UpdateFromDto(updateTagDto);

        await _dbContext.SaveChangesAsync(cancellationToken);

        Uri resourceUri = new Uri(Request.Path.Value!, UriKind.Relative);
        InMemoryETagStore.SetETag(resourceUri, tag.ToDto());
        Response.Headers.ETag = $"\"{InMemoryETagStore.GetETag(resourceUri)}\"";

        return NoContent();
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a tag")]
    [EndpointDescription("Permanently removes a tag from the system by its unique identifier.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTag(
        string id,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        Tag? tag = await _dbContext.Tags
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (tag is null)
        {
            return NotFound();
        }

        _dbContext.Tags.Remove(tag);

        await _dbContext.SaveChangesAsync(cancellationToken);

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
        bool hasNextPage,
        int tagsCount)
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

        if (tagsCount < 5)
        {
            links.Add(_linkService.Create(nameof(CreateTag), LinkRelations.Create, HttpMethods.Post));
        }

        return links;
    }
}
