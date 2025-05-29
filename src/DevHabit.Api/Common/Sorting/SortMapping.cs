namespace DevHabit.Api.Common.Sorting;

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

        return sortFields
            .Select(x => x[0] == '-' ? x[1..] : x)
            .All(x => validFields.Contains(x));
    }
}
