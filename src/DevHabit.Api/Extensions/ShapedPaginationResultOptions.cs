using DevHabit.Api.Services;

namespace DevHabit.Api.Extensions;

public sealed record ShapedPaginationResultOptions
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public string? Fields { get; init; }
    public required IDataShapingService DataShaping { get; init; }

    public void Deconstruct(
        out int page,
        out int pageSize,
        out string? fields,
        out IDataShapingService dataShaping)
    {
        page = Page;
        pageSize = PageSize;
        fields = Fields;
        dataShaping = DataShaping;
    }
}
