using DevHabit.Api.Dtos.Habits;
using DevHabit.Api.Entities;

namespace DevHabit.FunctionalTests.TestData;

public static class HabitsTestData
{
    public static CreateHabitDto CreateReadingHabitDto() => new()
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
}
