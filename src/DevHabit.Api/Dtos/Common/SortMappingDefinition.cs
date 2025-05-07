namespace DevHabit.Api.Dtos.Common;

public sealed class SortMappingDefinition<TSorce, TDestination>()
{
    public required ICollection<SortMapping> Mappings { get; init; }
}
