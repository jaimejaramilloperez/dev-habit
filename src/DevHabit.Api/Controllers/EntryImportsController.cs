using DevHabit.Api.Common.Auth;
using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Entries.ImportJob;
using DevHabit.Api.Entities;
using DevHabit.Api.Extensions;
using DevHabit.Api.Jobs.EntryImport;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/entries/imports")]
[Authorize(Roles = Roles.Member)]
[Produces(CustomMediaTypeNames.Application.HateoasJson)]
public sealed class EntryImportsController(
    ApplicationDbContext dbContext,
    UserContext userContext,
    LinkService linkService,
    ISchedulerFactory schedulerFactory) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly UserContext _userContext = userContext;
    private readonly LinkService _linkService = linkService;
    private readonly ISchedulerFactory _schedulerFactory = schedulerFactory;

    [HttpGet]
    public async Task<IActionResult> GetImportJobs(
        EntryImportsParameters entryImportsParameters,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (userId is null)
        {
            return Unauthorized();
        }

        var (fields, page, pageSize) = entryImportsParameters;

        ShapedPaginationResult<EntryImportJobDto> paginationResult = await _dbContext.EntryImportJobs.AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(EntryImportQueries.ProjectToDto())
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToShapedPaginationResultAsync(page, pageSize, fields, cancellationToken)
            .WithHateoasAsync(new()
            {
                ItemLinksFactory = x => CreateLinksForImportJob(x.Id),
                CollectionLinksFactory = x => CreateLinksForImportJobs(page, pageSize, x.HasPreviousPage, x.HasNextPage),
                AcceptHeader = entryImportsParameters.Accept,
            }, cancellationToken);

        return Ok(paginationResult);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetImportJob(
        string id,
        string? fields,
        AcceptHeaderDto acceptHeaderDto,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (userId is null)
        {
            return Unauthorized();
        }

        ShapedResult<EntryImportJobDto>? result = await _dbContext.EntryImportJobs
            .Where(x => x.Id == id && x.UserId == userId)
            .Select(EntryImportQueries.ProjectToDto())
            .ToShapedFirstOrDefaultAsync(fields, cancellationToken)
            .WithHateoasAsync(x => CreateLinksForImportJob(x.Id), acceptHeaderDto.Accept, cancellationToken);

        return result is null ? NotFound() : Ok(result.Item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateImportJob(
        CreateEntryImportJobDto createImportJob,
        AcceptHeaderDto acceptHeaderDto,
        IValidator<CreateEntryImportJobDto> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (userId is null)
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(createImportJob, cancellationToken);

        using MemoryStream memoryStream = new();
        await createImportJob.File.CopyToAsync(memoryStream, cancellationToken);

        EntryImportJob importJob = createImportJob.ToEntity(userId, memoryStream.ToArray());

        _dbContext.EntryImportJobs.Add(importJob);
        await _dbContext.SaveChangesAsync(cancellationToken);

        IScheduler scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

        IJobDetail jobDetail = JobBuilder.Create<ProcessEntryImportJob>()
            .WithIdentity($"process-entry-import-{importJob.Id}")
            .UsingJobData("importJobId", importJob.Id)
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity($"process-entry-import-trigger-{importJob.Id}")
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger, cancellationToken);

        EntryImportJobDto importJobDto = importJob.ToDto();

        if (HateoasHelpers.ShouldIncludeHateoas(acceptHeaderDto.Accept))
        {
            var result = DataShaper.ShapeData(importJobDto, CreateLinksForImportJob(importJob.Id));
            return CreatedAtAction(nameof(GetImportJob), new { id = importJobDto.Id }, result);
        }

        return CreatedAtAction(nameof(GetImportJob), new { id = importJobDto.Id }, importJobDto);
    }

    private ICollection<LinkDto> CreateLinksForImportJob(string id)
    {
        return
        [
            _linkService.Create(nameof(GetImportJob), LinkRelations.Self, HttpMethods.Get, new { id }),
        ];
    }

    private ICollection<LinkDto> CreateLinksForImportJobs(
        int page,
        int pageSize,
        bool hasPreviousPage,
        bool hasNextPage)
    {
        ICollection<LinkDto> links =
        [
            _linkService.Create(nameof(GetImportJobs), LinkRelations.Self, HttpMethods.Get, new
            {
                page = page,
                page_size = pageSize,
            }),
            _linkService.Create(nameof(CreateImportJob), LinkRelations.Create, HttpMethods.Post),
        ];

        if (hasPreviousPage)
        {
            links.Add(_linkService.Create(nameof(GetImportJobs), LinkRelations.PreviousPage, HttpMethods.Get, new
            {
                page = page - 1,
                page_size = pageSize,
            }));
        }

        if (hasNextPage)
        {
            links.Add(_linkService.Create(nameof(GetImportJobs), LinkRelations.NextPage, HttpMethods.Get, new
            {
                page = page + 1,
                page_size = pageSize,
            }));
        }

        return links;
    }
}
