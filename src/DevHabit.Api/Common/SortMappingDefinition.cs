namespace DevHabit.Api.Common;

public sealed class SortMappingDefinition<TSorce, TDestination>()
{
    public required ICollection<SortMapping> Mappings { get; init; }
}
