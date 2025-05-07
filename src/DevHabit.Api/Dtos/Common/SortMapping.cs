namespace DevHabit.Api.Dtos.Common;

public sealed record SortMapping(
    string SortField,
    string PropertyName,
    bool Reverse = false)
{
    public static bool AreAllSortFieldsValid(
        IEnumerable<SortMapping> mappings,
        IEnumerable<string> sortFields)
    {
        HashSet<string> validFields = new(
            mappings.Select(m => m.SortField),
            StringComparer.OrdinalIgnoreCase);

        return sortFields.All(validFields.Contains);
    }
}
