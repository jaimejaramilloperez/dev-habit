using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;
using DevHabit.Api.Common.Hateoas;
using DevHabit.Api.Dtos.Common;
using FluentValidation;

namespace DevHabit.Api.Common.DataShaping;

public static class DataShaper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = [];

    public static ExpandoObject ShapeData<T>(
        T entity,
        string? fields = null,
        ICollection<LinkDto>? links = null)
    {
        if (!AreAllFieldsValid<T>(fields))
        {
            throw new ValidationException([new("fields", $"Fields value '{fields}' is not valid")]);
        }

        HashSet<string> fieldsSet = fields?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        PropertyInfo[] propertyInfos = GetFilteredProperties<T>(fieldsSet);

        IDictionary<string, object?> shapedObject = new ExpandoObject();

        foreach (var propertyInfo in propertyInfos)
        {
            shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
        }

        if (links is not null)
        {
            shapedObject.TryAdd(HateoasPropertyNames.Links, links);
        }

        return (ExpandoObject)shapedObject;
    }

    public static ExpandoObject ShapeData<T>(
        T entity,
        ICollection<LinkDto>? links = null)
    {
        PropertyInfo[] propertyInfos = GetFilteredProperties<T>([]);

        IDictionary<string, object?> shapedObject = new ExpandoObject();

        foreach (var propertyInfo in propertyInfos)
        {
            shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
        }

        if (links is not null)
        {
            shapedObject.TryAdd(HateoasPropertyNames.Links, links);
        }

        return (ExpandoObject)shapedObject;
    }

    public static IReadOnlyCollection<ExpandoObject> ShapeCollectionData<T>(
        IReadOnlyCollection<T> entities,
        string? fields = null,
        Func<T, ICollection<LinkDto>>? linksFactory = null)
    {
        if (!AreAllFieldsValid<T>(fields))
        {
            throw new ValidationException([new("fields", $"Fields value '{fields}' is not valid")]);
        }

        HashSet<string> fieldsSet = fields?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        PropertyInfo[] propertyInfos = GetFilteredProperties<T>(fieldsSet);

        List<ExpandoObject> shapedObjects = new(entities.Count);

        foreach (var entity in entities)
        {
            IDictionary<string, object?> shapedObject = new ExpandoObject();

            foreach (var propertyInfo in propertyInfos)
            {
                shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
            }

            if (linksFactory is not null)
            {
                shapedObject.TryAdd(HateoasPropertyNames.Links, linksFactory(entity));
            }

            shapedObjects.Add((ExpandoObject)shapedObject);
        }

        return shapedObjects;
    }

    private static bool AreAllFieldsValid<T>(string? fields)
    {
        HashSet<string> fieldsSet = fields?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        string[] propertyNames = PropertiesCache
            .GetOrAdd(
                typeof(T),
                type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Select(x => x.Name)
            .ToArray();

        return fieldsSet.All(x => propertyNames.Contains(x, StringComparer.OrdinalIgnoreCase));
    }

    private static PropertyInfo[] GetFilteredProperties<T>(HashSet<string> fieldsSet)
    {
        PropertyInfo[] properties = PropertiesCache.GetOrAdd(
            typeof(T),
            type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        return fieldsSet.Count > 0
            ? properties.Where(x => fieldsSet.Contains(x.Name)).ToArray()
            : properties;
    }
}
