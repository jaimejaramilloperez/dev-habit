using DevHabit.Api.Services;

namespace DevHabit.Api.Common;

internal static class HateoasHelpers
{
    public static bool ShouldIncludeHateoas(string? acceptHeader)
    {
        return string.Equals(
            acceptHeader,
            CustomMediaTypes.Application.HateoasJson,
            StringComparison.OrdinalIgnoreCase);
    }
}

