namespace DevHabit.Api.Dtos.Entries;

public sealed record EntryStatsDto
{
    public IReadOnlyCollection<EntryDailyStatDto> DailyStats { get; init; } = [];
    public int TotalEntries { get; init; }
    public int CurrentStreak { get; init; }
    public int LongestStreak { get; init; }
}

public sealed record EntryDailyStatDto
{
    public DateOnly Date { get; init; }
    public int Count { get; init; }
}
