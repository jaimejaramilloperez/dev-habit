using DevHabit.Api.Dtos.Tags;

namespace DevHabit.FunctionalTests.TestData;

public static class TagsTestData
{
    public static CreateTagDto CreateTagDto() => new()
    {
        Name = "Productivity",
        Description = "Productivity related habits"
    };
}
