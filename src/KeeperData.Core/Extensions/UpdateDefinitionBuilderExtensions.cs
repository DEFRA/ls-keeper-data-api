using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Reflection;

namespace KeeperData.Core.Extensions;

public static class UpdateDefinitionBuilderExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> s_cachedProps = new();

    public static UpdateDefinition<T> SetAll<T>(this UpdateDefinitionBuilder<T> builder, T entity)
    {
        var type = typeof(T);

        var props = s_cachedProps.GetOrAdd(type, t =>
            [.. t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name != "Id")]
        );

        var updates = new List<UpdateDefinition<T>>();

        foreach (var prop in props)
        {
            var value = prop.GetValue(entity);
            updates.Add(builder.Set(prop.Name, value));
        }

        return builder.Combine(updates);
    }
}