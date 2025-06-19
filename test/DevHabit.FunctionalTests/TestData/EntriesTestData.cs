using DevHabit.Api.Dtos.Entries;

namespace DevHabit.FunctionalTests.TestData;

public static class EntriesTestData
{
    public static CreateEntryDto CreateEntryDto(
        string habitId,
        int value,
        DateOnly? date = null,
        string? notes = null)
    {
        return new()
        {
            HabitId = habitId,
            Date = date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Notes = notes,
            Value = value,
        };
    }
}
