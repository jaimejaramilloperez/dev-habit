using DevHabit.Api.Common.Sorting;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Tags;

internal static class TagMappings
{
    public static readonly SortMappingDefinition<TagDto, Tag> SortMapping = new()
    {
        Mappings = [
            new SortMapping(nameof(TagDto.Id), nameof(Tag.Id)),
            new SortMapping(nameof(TagDto.Name), nameof(Tag.Name)),
            new SortMapping(nameof(TagDto.Description), nameof(Tag.Description)),
            new SortMapping(nameof(TagDto.CreatedAtUtc), nameof(Tag.CreatedAtUtc)),
            new SortMapping(nameof(TagDto.UpdatedAtUtc), nameof(Tag.UpdatedAtUtc)),
        ],
    };

    public static Tag ToEntity(this CreateTagDto dto)
    {
        return new()
        {
            Id = $"t_{Guid.CreateVersion7()}",
            Name = dto.Name,
            Description = dto.Description,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public static TagDto ToDto(this Tag tag)
    {
        return new()
        {
            Id = tag.Id,
            Name = tag.Name,
            Description = tag.Description,
            CreatedAtUtc = tag.CreatedAtUtc,
            UpdatedAtUtc = tag.UpdatedAtUtc,
        };
    }

    public static void UpdateFromDto(this Tag tag, UpdateTagDto dto)
    {
        tag.Name = dto.Name;
        tag.Description = dto.Description;
        tag.UpdatedAtUtc = DateTime.UtcNow;
    }
}
