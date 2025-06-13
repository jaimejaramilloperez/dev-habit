using DevHabit.Api.Dtos.Common;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Entries.ImportJob;

public sealed record EntryImportsParameters : AcceptHeaderDto
{
    public string? Fields { get; init; }

    public int Page { get; init; } = 1;

    [FromQuery(Name = "page_size")]
    public int PageSize { get; init; } = 10;

    public void Deconstruct(out string? fields, out int page, out int pageSize)
    {
        fields = Fields;
        page = Page;
        pageSize = PageSize;
    }
}
