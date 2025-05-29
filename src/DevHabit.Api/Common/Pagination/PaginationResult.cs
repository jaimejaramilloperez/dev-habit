using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Common.Pagination;

public sealed record PaginationResult<T> : ICollectionResponse<T>, IPaginationResult, ILinksResponse
{
    public required IReadOnlyCollection<T> Data { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public long TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public IReadOnlyCollection<LinkDto> Links { get; init; } = [];
}
