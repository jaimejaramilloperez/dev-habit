using System.Dynamic;

namespace DevHabit.Api.Dtos.Common;

public sealed record ShapedPaginationResult : IShapedCollectionResponse, ILinksResponse
{
    public required ICollection<ExpandoObject> Data { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public long TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public List<LinkDto> Links { get; init; } = [];
}
