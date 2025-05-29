namespace DevHabit.Api.Common.Pagination;

public interface ICollectionResponse<T>
{
    public IReadOnlyCollection<T> Data { get; init; }
}
