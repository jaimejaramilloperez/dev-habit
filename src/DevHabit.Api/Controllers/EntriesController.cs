using System.Net.Mime;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Common.Idempotency;
using DevHabit.Api.Common.Pagination;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/entries")]
[Authorize(Roles = Roles.Member)]
[RequireUserId]
[EnableRateLimiting("default")]
[Produces(MediaTypeNames.Application.Json, CustomMediaTypeNames.Application.HateoasJson)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class EntriesController(
    ApplicationDbContext dbContext,
    LinkService linkService) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly LinkService _linkService = linkService;

    [HttpGet]
    [EndpointSummary("Get all entries")]
    [EndpointDescription("Retrieves a paginated list of entries with optional filtering by habit, date range, source, archive status, sorting, and field selection.")]
    [ProducesResponseType<PaginationResult<EntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEntries(
        EntriesParameters entriesParameters,
        IValidator<EntriesParameters> validator,
        CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        await validator.ValidateAndThrowAsync(entriesParameters, cancellationToken);

        var (habitId, fromDate, toDate, sort, fields, source, isArchived, page, pageSize) = entriesParameters;

        ShapedPaginationResult<EntryDto> paginationResult = await _dbContext.Entries.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => habitId == null || x.HabitId == habitId)
            .Where(x => fromDate == null || x.Date >= fromDate)
            .Where(x => toDate == null || x.Date <= toDate)
            .Where(x => source == null || x.Source == source)
            .Where(x => isArchived == null || x.IsArchived == isArchived)
            .SortByQueryString(sort, EntryMappings.SortMapping.Mappings)
            .Select(EntryQueries.ProjectToDto())
            .ToShapedPaginationResultAsync(page, pageSize, fields, cancellationToken)
            .WithHateoasAsync(new()
            {
                ItemLinksFactory = x => CreateLinksForEntry(x.Id, fields, x.IsArchived),
                CollectionLinksFactory = x => CreateLinksForEntries(entriesParameters, x.HasPreviousPage, x.HasNextPage),
                AcceptHeader = entriesParameters.Accept,
            }, cancellationToken);

        return Ok(paginationResult);
    }

    [HttpGet("cursor")]
    [EndpointSummary("Get entries using cursor pagination")]
    [EndpointDescription("Retrieves a list of entries using cursor-based pagination for efficient navigation through large datasets.")]
    [ProducesResponseType<CollectionResult<EntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEntriesCursor(
        EntriesCursorParameters entriesParameters,
        IValidator<EntriesCursorParameters> validator,
        CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        await validator.ValidateAndThrowAsync(entriesParameters, cancellationToken);

        var (habitId, fromDate, toDate, encodedCursor, fields, source, isArchived, limit) = entriesParameters;

        IQueryable<Entry> entriesQuery = _dbContext.Entries.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => habitId == null || x.HabitId == habitId)
            .Where(x => fromDate == null || x.Date >= fromDate)
            .Where(x => toDate == null || x.Date <= toDate)
            .Where(x => source == null || x.Source == source)
            .Where(x => isArchived == null || x.IsArchived == isArchived);

        if (!string.IsNullOrWhiteSpace(encodedCursor))
        {
            EntryCursorDto? cursor = EntryCursorDto.Decode(encodedCursor);

            if (cursor is not null)
            {
                entriesQuery = entriesQuery.Where(x =>
                    x.Date < cursor.Date ||
                    x.Date == cursor.Date && string.Compare(x.Id, cursor.Id) <= 0);
            }
        }

        CollectionResult<EntryDto> paginationResult = await entriesQuery
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Id)
            .Select(EntryQueries.ProjectToDto())
            .ToCursorPaginationResultAsync(limit, cancellationToken);

        List<EntryDto> items = paginationResult.Data.ToList();
        bool hasNextPage = items.Count > limit;
        string? nextCursor = null;

        if (hasNextPage)
        {
            EntryDto lastItem = items[^1];
            items.RemoveAt(items.Count - 1);
            nextCursor = EntryCursorDto.Encode(lastItem.Id, lastItem.Date);
        }

        List<LinkDto> links = [.. paginationResult.Links];

        bool shouldIncludeHateoas = HateoasHelpers.ShouldIncludeHateoas(entriesParameters.Accept);

        if (shouldIncludeHateoas)
        {
            links.AddRange(CreateLinksForEntriesCursor(entriesParameters, nextCursor));
        }

        ShapedCollectionResult<EntryDto> shapedPaginationResult = new()
        {
            Data = DataShaper.ShapeCollectionData(
                items,
                fields,
                shouldIncludeHateoas ? x => CreateLinksForEntry(x.Id, fields, x.IsArchived) : null),
            OriginalData = items,
            Links = links,
        };

        return Ok(shapedPaginationResult);
    }

    [HttpGet("{id}")]
    [EndpointSummary("Get an entry by ID")]
    [EndpointDescription("Retrieves a specific entry by its unique identifier with optional field selection.")]
    [ProducesResponseType<EntryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntry(
        string id,
        EntryParameters entryParameters,
        CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        string? fields = entryParameters.Fields;

        ShapedResult<EntryDto>? result = await _dbContext.Entries.AsNoTracking()
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(EntryQueries.ProjectToDto())
            .ToShapedFirstOrDefaultAsync(fields, cancellationToken)
            .WithHateoasAsync(x => CreateLinksForEntry(id, fields, x.IsArchived), entryParameters.Accept, cancellationToken);

        return result is null ? NotFound() : Ok(result.Item);
    }

    [HttpPost]
    [IdempotentRequest]
    [EndpointSummary("Create a new entry")]
    [EndpointDescription("Creates a new entry for a specific habit with the provided details.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<EntryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EntryDto>> CreateEntry(
        CreateEntryDto createEntryDto,
        AcceptHeaderDto acceptHeaderDto,
        IValidator<CreateEntryDto> validator,
        CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        await validator.ValidateAndThrowAsync(createEntryDto, cancellationToken);

        Habit? habit = await _dbContext.Habits
            .FirstOrDefaultAsync(x => x.Id == createEntryDto.HabitId && x.UserId == userId, cancellationToken);

        if (habit is null)
        {
            return Problem(
                detail: $"Habit with ID '{createEntryDto.HabitId}' does not exist",
                statusCode: StatusCodes.Status400BadRequest);
        }

        Entry entry = createEntryDto.ToEntity(userId);

        _dbContext.Entries.Add(entry);

        await _dbContext.SaveChangesAsync(cancellationToken);

        EntryDto entryDto = entry.ToDto();

        if (HateoasHelpers.ShouldIncludeHateoas(acceptHeaderDto.Accept))
        {
            var result = DataShaper.ShapeData(entryDto, CreateLinksForEntry(entryDto.Id));
            return CreatedAtAction(nameof(GetEntry), new { id = entryDto.Id }, result);
        }

        return CreatedAtAction(nameof(GetEntry), new { id = entryDto.Id }, entryDto);
    }

    [HttpPost("batch")]
    [EndpointSummary("Create multiple entries")]
    [EndpointDescription("Creates multiple entries in a single request. All entries must be valid and reference existing habits.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType<IEnumerable<EntryDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEntryBatch(
        CreateEntryBatchDto createEntryBatchDto,
        AcceptHeaderDto acceptHeaderDto,
        IValidator<CreateEntryBatchDto> validator,
        CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        await validator.ValidateAndThrowAsync(createEntryBatchDto, cancellationToken);

        HashSet<string> habitIds = createEntryBatchDto.Entries
            .Select(x => x.HabitId)
            .ToHashSet();

        List<Habit> existingHabits = await _dbContext.Habits
            .AsNoTracking()
            .Where(x => habitIds.Contains(x.Id) && x.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existingHabits.Count != habitIds.Count)
        {
            return Problem(
                detail: "One or more habit IDs are invalid",
                statusCode: StatusCodes.Status400BadRequest);
        }

        List<Entry> entries = createEntryBatchDto.Entries
            .Select(x => x.ToEntity(userId))
            .ToList();

        _dbContext.Entries.AddRange(entries);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (HateoasHelpers.ShouldIncludeHateoas(acceptHeaderDto.Accept))
        {
            var result = entries.Select(x =>
                DataShaper.ShapeData(x, CreateLinksForEntry(x.Id, null, x.IsArchived)))
                .ToList();

            return CreatedAtAction(nameof(GetEntries), result);
        }

        List<EntryDto> entryDtos = entries.Select(x => x.ToDto()).ToList();

        return CreatedAtAction(nameof(GetEntries), entryDtos);
    }

    [HttpPut("{id}")]
    [EndpointSummary("Update an entry")]
    [EndpointDescription("Updates an existing entry's details with the provided information.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEntry(
        string id,
        UpdateEntryDto updateEntryDto,
        IValidator<UpdateEntryDto> validator,
        CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        await validator.ValidateAndThrowAsync(updateEntryDto, cancellationToken);

        Entry? entry = await _dbContext.Entries
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        entry.UpdateFromDto(updateEntryDto);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id}/archive")]
    [EndpointSummary("Archive an entry")]
    [EndpointDescription("Marks an entry as archived, preserving it but removing it from active view.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveEntry(string id, CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        Entry? entry = await _dbContext.Entries
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        entry.IsArchived = true;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPut("{id}/un-archive")]
    [EndpointSummary("Unarchive an entry")]
    [EndpointDescription("Restores an archived entry to active status.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnArchiveEntry(string id, CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        Entry? entry = await _dbContext.Entries
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        entry.IsArchived = false;
        entry.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete an entry")]
    [EndpointDescription("Permanently removes an entry from the system by its unique identifier.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(string id, CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        Entry? entry = await _dbContext.Entries
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

        if (entry is null)
        {
            return NotFound();
        }

        _dbContext.Entries.Remove(entry);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("stats")]
    [EndpointSummary("Get entry statistics")]
    [EndpointDescription("Retrieves statistical information about user's entries.")]
    [ProducesResponseType<EntryStatsDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        string userId = HttpContext.GetUserId();

        var entries = await _dbContext.Entries.Where(x => x.UserId == userId)
            .OrderBy(x => x.Date)
            .Select(x => new { x.Date })
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return Ok(new EntryStatsDto
            {
                DailyStats = [],
                TotalEntries = 0,
                CurrentStreak = 0,
                LongestStreak = 0,
            });
        }

        List<EntryDailyStatDto> dailyStats = entries.GroupBy(x => x.Date)
            .Select(x => new EntryDailyStatDto
            {
                Date = x.Key,
                Count = x.Count(),
            })
            .OrderByDescending(x => x.Date)
            .ToList();

        int currentStreak = 0;
        int longestStreak = 0;
        int currentCount = 0;

        List<DateOnly> dates = entries.Select(x => x.Date)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int i = dates.Count - 1; i >= 0; i--)
        {
            bool isConsecutive = (i == dates.Count - 1)
                ? dates[i] == today
                : dates[i].AddDays(1) == dates[i + 1];

            if (isConsecutive)
            {
                currentStreak++;
            }
        }

        for (int i = 0; i < dates.Count; i++)
        {
            if (i == 0 || dates[i] == dates[i - 1].AddDays(1))
            {
                currentCount++;
                longestStreak = Math.Max(longestStreak, currentCount);
            }
            else
            {
                currentCount = 1;
            }
        }

        return Ok(new EntryStatsDto
        {
            DailyStats = dailyStats,
            TotalEntries = entries.Count,
            CurrentStreak = currentStreak,
            LongestStreak = longestStreak,
        });
    }

    private ICollection<LinkDto> CreateLinksForEntry(string id, string? fields = null, bool isArchived = false) =>
    [
        _linkService.Create(nameof(GetEntry), LinkRelations.Self, HttpMethods.Get, new { id, fields }),
        _linkService.Create(nameof(UpdateEntry), LinkRelations.Update, HttpMethods.Put, new { id }),
        _linkService.Create(nameof(DeleteEntry), LinkRelations.Delete, HttpMethods.Delete, new { id }),

        isArchived
            ? _linkService.Create(nameof(UnArchiveEntry), LinkRelations.UnArchive, HttpMethods.Put, new { id })
            : _linkService.Create(nameof(ArchiveEntry), LinkRelations.Archive, HttpMethods.Put, new { id }),
    ];

    private ICollection<LinkDto> CreateLinksForEntries(
        EntriesParameters parameters,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        ICollection<LinkDto> links =
        [
            _linkService.Create(nameof(GetEntries), LinkRelations.Self, HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                sort = parameters.Sort,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                page = parameters.Page,
                page_size = parameters.PageSize,
            }),
            _linkService.Create(nameof(GetStats), LinkRelations.Stats, HttpMethods.Get),
            _linkService.Create(nameof(CreateEntry), LinkRelations.Create, HttpMethods.Post),
            _linkService.Create(nameof(CreateEntryBatch), LinkRelations.CreateBatch, HttpMethods.Post),
        ];

        if (hasPreviousPage)
        {
            links.Add(_linkService.Create(nameof(GetEntries), LinkRelations.PreviousPage, HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                sort = parameters.Sort,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                page = parameters.Page - 1,
                page_size = parameters.PageSize,
            }));
        }

        if (hasNextPage)
        {
            links.Add(_linkService.Create(nameof(GetEntries), LinkRelations.NextPage, HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                sort = parameters.Sort,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                page = parameters.Page + 1,
                page_size = parameters.PageSize,
            }));
        }

        return links;
    }

    private ICollection<LinkDto> CreateLinksForEntriesCursor(
        EntriesCursorParameters parameters,
        string? nextCursor)
    {
        ICollection<LinkDto> links =
        [
            _linkService.Create(nameof(GetEntriesCursor), LinkRelations.Self, HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                cursor = parameters.Cursor,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                limit = parameters.Limit,
            }),
            _linkService.Create(nameof(GetStats), LinkRelations.Stats, HttpMethods.Get),
            _linkService.Create(nameof(CreateEntry), LinkRelations.Create, HttpMethods.Post),
            _linkService.Create(nameof(CreateEntryBatch), LinkRelations.CreateBatch, HttpMethods.Post),
        ];

        if (!string.IsNullOrWhiteSpace(nextCursor))
        {
            links.Add(_linkService.Create(nameof(GetEntriesCursor), LinkRelations.NextPage, HttpMethods.Get, new
            {
                habit_id = parameters.HabitId,
                from_date = parameters.FromDate,
                to_date = parameters.ToDate,
                cursor = nextCursor,
                fields = parameters.Fields,
                source = parameters.Source,
                is_archived = parameters.IsArchived,
                limit = parameters.Limit,
            }));
        }

        return links;
    }
}
