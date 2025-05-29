namespace DevHabit.Api.Common.Sorting;

public sealed class SortMappingDefinition<TSorce, TDestination>()
{
    public required ICollection<SortMapping> Mappings { get; init; }
}
