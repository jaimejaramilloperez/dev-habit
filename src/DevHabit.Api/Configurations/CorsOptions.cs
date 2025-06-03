namespace DevHabit.Api.Configurations;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "DevHabitCorsPolicy";
    public required IEnumerable<string> AllowedOrigins { get; init; }
}
