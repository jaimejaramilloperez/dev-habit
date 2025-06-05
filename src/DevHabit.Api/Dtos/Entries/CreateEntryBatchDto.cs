namespace DevHabit.Api.Dtos.Entries;

public sealed record CreateEntryBatchDto
{
    public required List<CreateEntryDto> Entries { get; init; }
}
