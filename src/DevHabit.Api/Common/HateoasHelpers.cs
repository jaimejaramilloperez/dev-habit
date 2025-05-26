using DevHabit.Api.Services;

namespace DevHabit.Api.Common;

internal static class HateoasHelpers
{
    // public static bool ShouldIncludeHateoas(string? acceptHeader)
    // {
    //     return string.Equals(
    //         acceptHeader,
    //         CustomMediaTypesNames.Application.HateoasJson,
    //         StringComparison.OrdinalIgnoreCase);
    // }

    public static bool ShouldIncludeHateoas(string? acceptHeader)
    {
        return CustomMediaTypesNames.Application.HateoasMediaTypes.Any(
            mediaType => string.Equals(mediaType, acceptHeader, StringComparison.OrdinalIgnoreCase));
    }
}

