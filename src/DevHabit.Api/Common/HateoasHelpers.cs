using DevHabit.Api.Services;

namespace DevHabit.Api.Common;

internal static class HateoasHelpers
{
    public static bool ShouldIncludeHateoas(string? acceptHeader)
    {
        return acceptHeader is not null &&
            CustomMediaTypesNames.Application.HateoasMediaTypes.Contains(acceptHeader);
    }
}

