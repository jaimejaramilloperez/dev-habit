using DevHabit.Api.Dtos.HabitTags;

namespace DevHabit.FunctionalTests.TestData;

public static class HabitTagsTestData
{
    public static UpsertHabitTagsDto CreateUpsertDto(IReadOnlyCollection<string> tagIds) => new()
    {
        TagIds = tagIds,
    };
}
