namespace DevHabit.Api.Dtos.Common;

public interface ICollectionResponse<T>
{
    public ICollection<T> Data { get; init; }
}
