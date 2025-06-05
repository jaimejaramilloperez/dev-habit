using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Entries;

public sealed record EntriesParameters : AcceptHeaderDto
{
    [FromQuery(Name = "habit_id")]
    public string? HabitId { get; init; }

    [FromQuery(Name = "from_date")]
    public DateOnly? FromDate { get; init; }

    [FromQuery(Name = "to_date")]
    public DateOnly? ToDate { get; init; }

    public string? Sort { get; init; }

    public string? Fields { get; init; }

    public EntrySource? Source { get; init; }

    [FromQuery(Name = "is_archive")]
    public bool? IsArchived { get; init; }

    public int Page { get; init; } = 1;

    [FromQuery(Name = "page_size")]
    public int PageSize { get; init; } = 10;

    public void Deconstruct(
        out string? habitId,
        out DateOnly? fromDate,
        out DateOnly? toDate,
        out string? sort,
        out string? fields,
        out EntrySource? source,
        out bool? isArchived,
        out int page,
        out int pageSize)
    {
        habitId = HabitId;
        fromDate = FromDate;
        toDate = ToDate;
        sort = Sort;
        fields = Fields;
        source = Source;
        isArchived = IsArchived;
        page = Page;
        pageSize = PageSize;
    }
}
