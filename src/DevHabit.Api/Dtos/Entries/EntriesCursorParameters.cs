using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Entries;

public sealed record EntriesCursorParameters : AcceptHeaderDto
{
    [FromQuery(Name = "habit_id")]
    public string? HabitId { get; init; }

    [FromQuery(Name = "from_date")]
    public DateOnly? FromDate { get; init; }

    [FromQuery(Name = "to_date")]
    public DateOnly? ToDate { get; init; }

    public string? Cursor { get; init; }

    public string? Fields { get; init; }

    public EntrySource? Source { get; init; }

    [FromQuery(Name = "is_archive")]
    public bool? IsArchived { get; init; }

    public int Limit { get; init; } = 10;

    public void Deconstruct(
        out string? habitId,
        out DateOnly? fromDate,
        out DateOnly? toDate,
        out string? cursor,
        out string? fields,
        out EntrySource? source,
        out bool? isArchived,
        out int limit)
    {
        habitId = HabitId;
        fromDate = FromDate;
        toDate = ToDate;
        cursor = Cursor;
        fields = Fields;
        source = Source;
        isArchived = IsArchived;
        limit = Limit;
    }
}
