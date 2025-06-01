using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Habits;

public sealed record HabitsParameters : AcceptHeaderDto
{
    [FromQuery(Name = "q")]
    public string? SearchTerm { get; init; }

    public HabitType? Type { get; init; }

    public HabitStatus? Status { get; init; }

    public string? Sort { get; init; }

    public string? Fields { get; init; }

    public int Page { get; init; } = 1;

    [FromQuery(Name = "page_size")]
    public int PageSize { get; init; } = 10;

    public void Deconstruct(
        out string? searchTerm,
        out HabitType? type,
        out HabitStatus? status,
        out string? sort,
        out string? fields,
        out int page,
        out int pageSize)
    {
        searchTerm = SearchTerm;
        type = Type;
        status = Status;
        sort = Sort;
        fields = Fields;
        page = Page;
        pageSize = PageSize;
    }
}
