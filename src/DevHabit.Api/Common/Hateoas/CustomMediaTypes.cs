namespace DevHabit.Api.Common.Hateoas;

internal static class CustomMediaTypeNames
{
    internal static class Application
    {
        public const string JsonV1 = "application/json;v=1";
        public const string JsonV2 = "application/json;v=2";
        public const string HateoasJson = "application/vnd.dev-habit.hateoas+json";
        public const string HateoasJsonV1 = "application/vnd.dev-habit.hateoas.v1+json";
        public const string HateoasJsonV2 = "application/vnd.dev-habit.hateoas.v2+json";

        public static readonly HashSet<string> HateoasMediaTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            HateoasJson,
            HateoasJsonV1,
            HateoasJsonV2,
        };
    }
}
