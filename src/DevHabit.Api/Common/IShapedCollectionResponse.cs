using System.Dynamic;

namespace DevHabit.Api.Common;

public interface IShapedCollectionResponse
{
    public ICollection<ExpandoObject> Data { get; init; }
}
