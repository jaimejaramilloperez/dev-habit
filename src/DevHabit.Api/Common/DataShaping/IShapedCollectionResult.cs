using System.Dynamic;

namespace DevHabit.Api.Common.DataShaping;

public interface IShapedCollectionResult
{
    public ICollection<ExpandoObject> Data { get; init; }
}
