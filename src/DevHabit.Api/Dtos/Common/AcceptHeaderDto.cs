using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Dtos.Common;

public record AcceptHeaderDto
{
    [FromHeader(Name = "Accept")]
    public string? Accept { get; init; }
}
