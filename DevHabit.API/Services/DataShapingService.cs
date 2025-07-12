using System.Collections.Concurrent;
using System.Dynamic;
using System.Reflection;

namespace DevHabit.API.Services;

public sealed class DataShapingService
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertiesCache = new();

    // for one object
    public ExpandoObject ShapeData<T>(T entity, string? fields)
    {
        HashSet<string> fieldsSet = fields?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        PropertyInfo[] propertyInfos = PropertiesCache.GetOrAdd(
            typeof(T),
            t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        if (fieldsSet.Any())
        {
            propertyInfos = propertyInfos
                .Where(p => fieldsSet.Contains(p.Name))
                .ToArray();
        }

        IDictionary<string, object?> shapedObject = new ExpandoObject();

        foreach(PropertyInfo propertyInfo in propertyInfos)
        {
            shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
        }

        return (ExpandoObject)shapedObject;
    }

    public List<ExpandoObject> ShapeCollectionData<T>(IEnumerable<T> entities, string? fields)
    {
        HashSet<string> fieldsSet = fields?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        PropertyInfo[] propertyInfos = PropertiesCache.GetOrAdd(
            typeof(T),
            t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        if (fieldsSet.Any())
        {
            propertyInfos = propertyInfos
                .Where(p => fieldsSet.Contains(p.Name))
                .ToArray();
        }

        List<ExpandoObject> shapedObjects = [];

        foreach(T entity in entities)
        {
            IDictionary<string, object?> shapedObject = new ExpandoObject();

            foreach(PropertyInfo propertyInfo in propertyInfos)
            {
                shapedObject[propertyInfo.Name] = propertyInfo.GetValue(entity);
            }

            shapedObjects.Add((ExpandoObject)shapedObject);
        }

        return shapedObjects;
    }

    public bool Validate<T>(string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return true;
        }

        PropertyInfo[] propertyInfos = PropertiesCache.GetOrAdd(
          typeof(T),
          t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        HashSet<string> fieldsSet = fields?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        return fieldsSet.All(f => propertyInfos.Any(p => p.Name.Equals(f, StringComparison.OrdinalIgnoreCase)));
    }
}
