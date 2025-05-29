using System.Dynamic;

namespace DevHabit.Api.Common.DataShaping;

public interface IShapedCollectionResponse
{
    public IReadOnlyCollection<ExpandoObject> Data { get; init; }
}
