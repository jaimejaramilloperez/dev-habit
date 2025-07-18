using DevHabit.Api.Dtos.Common;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Tags;

public sealed record TagsParameters : AcceptHeaderDto
{
    [FromQuery(Name = "q")]
    public string? SearchTerm { get; init; }

    public string? Sort { get; init; }

    public string? Fields { get; init; }

    public int Page { get; init; } = 1;

    [FromQuery(Name = "page_size")]
    public int PageSize { get; init; } = 10;

    public void Deconstruct(
        out string? searchTerm,
        out string? sort,
        out string? fields,
        out int page,
        out int pageSize)
    {
        searchTerm = SearchTerm;
        sort = Sort;
        fields = Fields;
        page = Page;
        pageSize = PageSize;
    }
}
