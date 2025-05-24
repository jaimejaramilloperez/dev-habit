using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Tags;

public sealed record TagsParameters
{
    [FromQuery(Name = "q")]
    public string? SearchTerm { get; init; }

    public string? Sort { get; init; }

    public string? Fields { get; init; }

    public int Page { get; init; } = 1;

    [FromQuery(Name = "page_size")]
    public int PageSize { get; init; } = 10;

    [FromHeader(Name = "Accept")]
    public string? Accept { get; set; }
}
