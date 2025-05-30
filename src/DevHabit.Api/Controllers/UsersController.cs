using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Users;
using DevHabit.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(
    ApplicationDbContext dbContext,
    UserContext userContext) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly UserContext _userContext = userContext;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        string? userId = await _userContext.GetUserIdAsync();

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
            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        string? userId = await _userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        UserDto? user = await _dbContext.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }
}
