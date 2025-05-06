namespace DevHabit.Api.Dtos.Common;

public sealed record PaginationResult<T> : ICollectionResponse<T>
{
    public required List<T> Data { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public long TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
