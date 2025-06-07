using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Dtos.Common;

namespace DevHabit.Api.Common.Pagination;

public sealed record CollectionResult<T> : ICollectionResult<T>, ILinksResponse
{
    public required IReadOnlyCollection<T> Data { get; init; }
    public IReadOnlyCollection<LinkDto> Links { get; init; } = [];
}
