using DevHabit.Api.Database;
using DevHabit.Api.Dtos.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController(ApplicationDbContext dbContext) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        UserDto? user = await _dbContext.Users.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(UserQueries.ProjectToDto())
            .FirstOrDefaultAsync();

        return user is null ? NotFound() : Ok(user);
    }
}
