namespace DevHabit.Api.Common.Pagination;

public interface ICollectionResult<T>
{
    public IReadOnlyCollection<T> Data { get; init; }
}
