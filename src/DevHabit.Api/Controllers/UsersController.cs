using System.Net.Mime;
using DevHabit.Api.Common.Auth;
using DevHabit.Api.Common.DataShaping;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Dtos.Users;
using DevHabit.Api.Entities;
using DevHabit.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Member)]
[Produces(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public sealed class UsersController(
    ApplicationDbContext dbContext,
    UserContext userContext,
    LinkService linkService) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly UserContext _userContext = userContext;
    private readonly LinkService _linkService = linkService;

    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Admin)]
    [EndpointSummary("Get a user by ID")]
    [EndpointDescription("Get a user by their unique identifier. This endpoint requires Admin role permissions.")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserById(string id, CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (id != userId)
        {
            return Forbid();
        }

        UserDto? user = await _dbContext.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("me")]
    [EndpointSummary("Get current user's profile")]
    [EndpointDescription("Get the profile information for the currently authenticated user.")]
    [Produces(CustomMediaTypeNames.Application.HateoasJson)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser(
        AcceptHeaderDto acceptHeaderDto,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        UserDto? user = await _dbContext.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (HateoasHelpers.ShouldIncludeHateoas(acceptHeaderDto.Accept))
        {
            var result = DataShaper.ShapeData(user, CreateLinksForUser());
            return Ok(result);
        }

        return Ok(user);
    }

    [HttpPut("me/profile")]
    [EndpointSummary("Update current user's profile")]
    [EndpointDescription("Updates the profile information for the currently authenticated user.")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile(
        UpdateProfileDto updateProfileDto,
        IValidator<UpdateProfileDto> validator,
        CancellationToken cancellationToken)
    {
        string? userId = await _userContext.GetUserIdAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await validator.ValidateAndThrowAsync(updateProfileDto, cancellationToken);

        User? user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        user.UpdateFromDto(updateProfileDto);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private ICollection<LinkDto> CreateLinksForUser()
    {
        return
        [
            _linkService.Create(nameof(GetCurrentUser), LinkRelations.Self, HttpMethods.Get),
            _linkService.Create(nameof(UpdateProfile), LinkRelations.UpdateProfile, HttpMethods.Put),
        ];
    }
}
