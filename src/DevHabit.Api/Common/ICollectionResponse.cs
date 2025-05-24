namespace DevHabit.Api.Common;

public interface ICollectionResponse<T>
{
    public ICollection<T> Data { get; init; }
}
