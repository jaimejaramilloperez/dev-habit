using DevHabit.Api.Dtos.Entries;
using DevHabit.Api.Entities;

namespace DevHabit.IntegrationTests.TestData;

public static class EntriesTestData
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    public static readonly CreateEntryDto ValidCreateEntryDto = new()
    {
        HabitId = Habit.CreateNewId(),
        Date = Today,
        Notes = "Completed reading 30 pages",
        Value = 30,
    };

    public static readonly CreateEntryDto InvalidCreateEntryDto = new()
    {
        HabitId = "",
        Date = Today.AddDays(2),
        Notes = "",
        Value = -1,
    };

    public static readonly UpdateEntryDto ValidUpdateEntryDto = new()
    {
        Notes = "Updated: Read 40 pages",
        Value = 40,
    };

    public static readonly UpdateEntryDto InvalidUpdateEntryDto = new()
    {
        Notes = "Updated: Read 40 pages",
        Value = -1,
    };

    public static CreateEntryBatchDto GetValidCreateEntryBatchDto(string? habitId = null) => new()
    {
        Entries =
        [
            new()
            {
                HabitId = habitId is null ? Habit.CreateNewId() : habitId,
                Date = Today,
                Notes = "Entry 1",
                Value = 30,
            },
            new()
            {
                HabitId = habitId is null ? Habit.CreateNewId() : habitId,
                Date = Today,
                Notes = "Entry 2",
                Value = 40,
            }
        ]
    };

    public static readonly CreateEntryBatchDto InvalidCreateEntryBatchDto = new()
    {
        Entries =
        [
            new()
            {
                HabitId = "",
                Date = Today,
                Notes = "",
                Value = -1,
            }
        ]
    };
}
