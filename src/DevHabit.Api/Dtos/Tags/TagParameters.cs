using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Tags;

public sealed record TagParameters
{
    public string? Fields { get; init; }

    [FromHeader(Name = "Accept")]
    public string? Accept { get; set; }
}
