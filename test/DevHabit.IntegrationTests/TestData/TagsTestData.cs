using DevHabit.Api.Dtos.Tags;

namespace DevHabit.IntegrationTests.TestData;

public static class TagsTestData
{
    public static readonly CreateTagDto ValidCreateTagDto = new()
    {
        Name = "Programming",
        Description = "Programming related habits"
    };

    public static readonly CreateTagDto InvalidCreateTagDto = new()
    {
        Name = "",
        Description = "Invalid tag with empty name"
    };

    public static readonly UpdateTagDto ValidUpdateTagDto = new()
    {
        Name = "Updated Tag Name",
        Description = "Updated tag description"
    };

    public static readonly UpdateTagDto InValidUpdateTagDto = new()
    {
        Name = "",
        Description = "Updated tag description"
    };
}
