namespace DevHabit.Api.Common.Pagination;

public interface ICollectionResult<T>
{
    public ICollection<T> Data { get; init; }
}
