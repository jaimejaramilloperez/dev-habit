using System.Dynamic;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Dtos.Common;
using Newtonsoft.Json;

namespace DevHabit.Api.Common.DataShaping;

public sealed class ShapedCollectionResult<T> : IShapedCollectionResult, ILinksResponse
{
    public required ICollection<ExpandoObject> Data { get; init; }
    [JsonIgnore] public ICollection<T> OriginalData { get; init; } = [];
    public IReadOnlyCollection<LinkDto> Links { get; init; } = [];
}
