using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace EasyJsonSchema;

internal static class MemberSelection
{
    internal static bool IsIncluded(PropertyInfo property)
    {
        if (property.GetIndexParameters().Length != 0)
        {
            return false;
        }

        if (property.GetMethod is not { IsPublic: true, IsStatic: false })
        {
            return false;
        }

        // An init accessor is a public setter. JsonSerializer can set it while reading.
        if (property.SetMethod is not { IsPublic: true, IsStatic: false })
        {
            return false;
        }

        return !IsIgnoredWhenReading(property);
    }

    internal static List<PropertyInfo> GetIncludedProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsIncluded)
            .OrderBy(property => property.DeclaringType == type ? 1 : 0)
            .ThenBy(property => property.MetadataToken)
            .ToList();
    }

    internal static void Apply(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        for (var index = typeInfo.Properties.Count - 1; index >= 0; index--)
        {
            var property = typeInfo.Properties[index];
            if (property.AttributeProvider is PropertyInfo info && IsIncluded(info))
            {
                property.ShouldSerialize = null;
                continue;
            }

            typeInfo.Properties.RemoveAt(index);
        }
    }

    #region private

    private static bool IsIgnoredWhenReading(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<JsonIgnoreAttribute>();
        if (attribute is null)
        {
            return false;
        }

        return attribute.Condition is JsonIgnoreCondition.Always or JsonIgnoreCondition.WhenReading;
    }

    #endregion
}
