using System.Dynamic;

namespace DevHabit.Api.Dtos.Common;

public interface IShapedCollectionResponse
{
    public ICollection<ExpandoObject> Data { get; init; }
}
