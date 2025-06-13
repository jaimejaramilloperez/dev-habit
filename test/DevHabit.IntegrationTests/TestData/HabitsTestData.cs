using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;

namespace DevHabit.IntegrationTests.TestData;

public static class HabitsTestData
{
    public static readonly CreateHabitDto ValidCreateHabitDto = new()
    {
        Name = "Read books",
        Description = "Read technical books to improve skills",
        Type = HabitType.Measurable,
        Frequency = new()
        {
            Type = FrequencyType.Daily,
            TimesPerPeriod = 1,
        },
        Target = new()
        {
            Value = 30,
            Unit = "pages",
        },
    };

    public static readonly CreateHabitDto InValidCreateHabitDto = new()
    {
        Name = "Read books",
        Description = "Read technical books to improve skills",
        Type = HabitType.Binary,
        Frequency = new()
        {
            Type = FrequencyType.None,
            TimesPerPeriod = -1,
        },
        Target = new()
        {
            Value = 30,
            Unit = "pages",
        },
    };

    public static readonly UpdateHabitDto ValidUpdateHabitDto = new()
    {
        Name = "Updated Habit",
        Description = "Updated Description",
        Type = HabitType.Measurable,
        Frequency = new()
        {
            Type = FrequencyType.Weekly,
            TimesPerPeriod = 3,
        },
        Target = new()
        {
            Value = 50,
            Unit = "pages",
        },
    };

    public static readonly UpdateHabitDto InValidUpdateHabitDto = new()
    {
        Name = "Updated Habit",
        Description = "Updated Description",
        Type = HabitType.Binary,
        Frequency = new()
        {
            Type = FrequencyType.None,
            TimesPerPeriod = -1,
        },
        Target = new()
        {
            Value = 50,
            Unit = "pages",
        },
    };
}
