using System.Dynamic;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Common.Pagination;
using DevHabit.Api.Dtos.Common;
using Newtonsoft.Json;

namespace DevHabit.Api.Common.DataShaping;

public sealed record ShapedPaginationResult<T> : IShapedCollectionResponse, IPaginationResult, ILinksResponse
{
    public required IReadOnlyCollection<ExpandoObject> Data { get; init; }
    [JsonIgnore] public required IReadOnlyCollection<T> OriginalData { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long TotalCount { get; init; }
    public long TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public IReadOnlyCollection<LinkDto> Links { get; init; } = [];
}
