namespace DevHabit.Api.Dtos.Common;

public interface ICollectionResponse<T>
{
    public List<T> Data { get; init; }
}
