using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;
using DevHabit.Api.Common;
using DevHabit.Api.Dtos.Common;
using FluentValidation;

namespace DevHabit.Api.Services.DataShapingServices;

public sealed class DataShapingService : IDataShapingService
{
    private readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertiesCache = [];

    public ExpandoObject ShapeData<T>(
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

        PropertyInfo[] propertyInfos = _propertiesCache.GetOrAdd(
            typeof(T),
            type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        if (fieldsSet.Count > 0)
        {
            propertyInfos = propertyInfos
                .Where(x => fieldsSet.Contains(x.Name))
                .ToArray();
        }

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

    public ICollection<ExpandoObject> ShapeCollectionData<T>(
        ICollection<T> entities,
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

        PropertyInfo[] propertyInfos = _propertiesCache.GetOrAdd(
            typeof(T),
            type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        if (fieldsSet.Count > 0)
        {
            propertyInfos = propertyInfos
                .Where(x => fieldsSet.Contains(x.Name))
                .ToArray();
        }

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

    private bool AreAllFieldsValid<T>(string? fields)
    {
        HashSet<string> fieldsSet = fields?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        string[] propertyNames = _propertiesCache
            .GetOrAdd(
                typeof(T),
                type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Select(p => p.Name)
            .ToArray();

        return fieldsSet.All(f => propertyNames.Contains(f, StringComparer.OrdinalIgnoreCase));
    }
}
