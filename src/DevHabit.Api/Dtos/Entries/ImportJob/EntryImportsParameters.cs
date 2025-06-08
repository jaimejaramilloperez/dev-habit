using DevHabit.Api.Dtos.Common;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Entries.ImportJob;

public sealed record EntryImportsParameters : AcceptHeaderDto
{
    public int Page { get; init; } = 1;

    [FromQuery(Name = "page_size")]
    public int PageSize { get; init; } = 10;

    public void Deconstruct(out int page, out int pageSize)
    {
        page = Page;
        pageSize = PageSize;
    }
}
