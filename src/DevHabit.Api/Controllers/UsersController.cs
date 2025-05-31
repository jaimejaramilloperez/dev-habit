using DevHabit.Api.Common.Auth;
using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Users;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Member)]
public sealed class UsersController(
    ApplicationDbContext dbContext,
    UserContext userContext) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly UserContext _userContext = userContext;

    [HttpGet("{id}")]
    [Authorize(Roles = Roles.Admin)]
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
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
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

        return user is null ? NotFound() : Ok(user);
    }
}
