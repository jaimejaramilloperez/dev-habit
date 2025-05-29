namespace DevHabit.Api.Common.Pagination;

public interface IPaginationResult
{
    int Page { get; init; }
    int PageSize { get; init; }
    long TotalCount { get; init; }
    long TotalPages { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
}
