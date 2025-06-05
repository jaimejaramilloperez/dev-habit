using DevHabit.Api.Common.Sorting;
using DevHabit.Api.Entities;

namespace DevHabit.Api.Dtos.Entries;

public static class EntryMappings
{
    public static readonly SortMappingDefinition<EntryDto, Entry> SortMapping = new()
    {
        Mappings = [
            new SortMapping(nameof(EntryDto.Id), nameof(Entry.Id)),
            new SortMapping(nameof(EntryDto.Value), nameof(Entry.Value)),
            new SortMapping(nameof(EntryDto.Date), nameof(Entry.Date)),
            new SortMapping(nameof(EntryDto.CreatedAtUtc), nameof(Entry.CreatedAtUtc)),
            new SortMapping(nameof(EntryDto.UpdatedAtUtc), nameof(Entry.UpdatedAtUtc)),
        ],
    };

    public static Entry ToEntity(this CreateEntryDto dto, string userId)
    {
        return new()
        {
            Id = $"e_{Guid.CreateVersion7()}",
            HabitId = dto.HabitId,
            UserId = userId,
            Value = dto.Value,
            Notes = dto.Notes,
            Source = EntrySource.Manual,
            ExternalId = null,
            IsArchived = false,
            Date = dto.Date,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public static EntryDto ToDto(this Entry entry)
    {
        return new()
        {
            Id = entry.Id,
            Value = entry.Value,
            Notes = entry.Notes,
            Source = entry.Source,
            ExternalId = entry.ExternalId,
            IsArchived = entry.IsArchived,
            Date = entry.Date,
            CreatedAtUtc = entry.CreatedAtUtc,
            UpdatedAtUtc = entry.UpdatedAtUtc,
        };
    }

    public static void UpdateFromDto(this Entry entry, UpdateEntryDto dto)
    {
        entry.Value = dto.Value;
        entry.Notes = dto.Notes;
        entry.UpdatedAtUtc = DateTime.UtcNow;
    }
}
