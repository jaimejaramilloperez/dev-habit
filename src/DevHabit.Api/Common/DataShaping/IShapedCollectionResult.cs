using System.Dynamic;

namespace DevHabit.Api.Common.DataShaping;

public interface IShapedCollectionResult
{
    public IReadOnlyCollection<ExpandoObject> Data { get; init; }
}
