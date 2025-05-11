using System.Linq.Expressions;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Tags;

internal static class TagQueries
{
    public static Expression<Func<Tag, TagDto>> ProjectToDto()
    {
        return tag => new()
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            CreatedAtUtc = tag.CreatedAtUtc,
            UpdatedAtUtc = tag.UpdatedAtUtc,
        };
    }
}
