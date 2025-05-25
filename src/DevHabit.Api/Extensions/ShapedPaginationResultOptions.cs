using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Extensions;

public sealed record ShapedPaginationResultOptions<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public string? Fields { get; init; }
    public required HttpContext HttpContext { get; init; }
    public Func<T, ICollection<LinkDto>>? LinksFactory { get; init; }

    public void Deconstruct(
        out int page,
        out int pageSize,
        out string? fields,
        out HttpContext httpContext,
        out Func<T, ICollection<LinkDto>>? linksFactory)
    {
        page = Page;
        pageSize = PageSize;
        fields = Fields;
        httpContext = HttpContext;
        linksFactory = LinksFactory;
    }
}
