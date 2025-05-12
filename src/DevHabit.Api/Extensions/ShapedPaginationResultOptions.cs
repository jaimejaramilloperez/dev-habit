using DevHabit.Api.Dtos.Common;
using DevHabit.Api.Services.DataShapingServices;

namespace DevHabit.Api.Extensions;

public sealed record ShapedPaginationResultOptions<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public string? Fields { get; init; }
    public required IDataShapingService DataShaping { get; init; }
    public Func<T, ICollection<LinkDto>>? LinksFactory { get; init; }

    public void Deconstruct(
        out int page,
        out int pageSize,
        out string? fields,
        out IDataShapingService dataShaping,
        out Func<T, ICollection<LinkDto>>? linksFactory)
    {
        page = Page;
        pageSize = PageSize;
        fields = Fields;
        dataShaping = DataShaping;
        linksFactory = LinksFactory;
    }
}
