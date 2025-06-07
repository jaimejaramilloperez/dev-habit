namespace DevHabit.Api.Configurations;

public sealed class GitHubAutomationOptions
{
    public const string SectionName = "Jobs";
    public int ScanIntervalInMinutes { get; init; }
}
