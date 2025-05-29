namespace DevHabit.Api.Common.Hateoas;

internal static class HateoasHelpers
{
    public static bool ShouldIncludeHateoas(string? acceptHeader)
    {
        return acceptHeader is not null &&
            CustomMediaTypesNames.Application.HateoasMediaTypes.Contains(acceptHeader);
    }
}

