using System.Linq.Expressions;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Users;

internal static class UserQueries
{
    public static Expression<Func<User, UserDto>> ProjectToDto()
    {
        return u => new()
        {
            Id = u.Id,
            Email = u.Email,
            Name = u.Name,
            CreatedAtUtc = u.CreatedAtUtc,
            UpdatedAtUtc = u.UpdatedAtUtc,
        };
    }
}
