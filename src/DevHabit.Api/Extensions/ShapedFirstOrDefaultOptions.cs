using DevHabit.Api.Dtos.Common;
using Microsoft.Extensions.Primitives;

namespace DevHabit.Api.Extensions;

public sealed record ShapedFirstOrDefaultOptions
{
    public string? Fields { get; init; }
    public required ICollection<LinkDto> Links { get; init; }
    public required HttpContext HttpContext { get; init; }
    public StringValues AcceptHeader => HttpContext.Request.Headers.Accept;

    public void Deconstruct(
        out string? fields,
        out ICollection<LinkDto> links,
        out StringValues acceptHeader,
        out HttpContext httpContext)
    {
        fields = Fields;
        links = Links;
        acceptHeader = AcceptHeader;
        httpContext = HttpContext;
    }
}
