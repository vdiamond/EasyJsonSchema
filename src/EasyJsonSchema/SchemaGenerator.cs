using System.Reflection;
using System.Text.Json.Nodes;

namespace EasyJsonSchema;

internal static class SchemaGenerator
{
    internal static bool TryGenerate(Type type, out string errorOrSchema)
    {
        var context = new NullabilityInfoContext();
        if (!TryBuildProperties(
            type,
            childDefinitionLevel: 1,
            parentChain: string.Empty,
            parentIsArray: false,
            context,
            out var properties,
            out var error))
        {
            errorOrSchema = error;
            return false;
        }

        var root = new JsonObject
        {
            ["properties"] = properties,
        };
        errorOrSchema = root.ToJsonString(SerializerOptions.Compact);
        return true;
    }

    #region private

    private const int FirstDisallowedDefinitionLevel = 4;

    private static bool TryBuildProperties(
        Type type,
        int childDefinitionLevel,
        string parentChain,
        bool parentIsArray,
        NullabilityInfoContext context,
        out JsonArray properties,
        out string error)
    {
        properties = [];
        var declared = GetProperties(type);
        if (declared.Count == 0)
        {
            if (parentChain.Length == 0)
            {
                error = Messages.Problem("$", Messages.AtLeastOneProperty);
            }
            else
            {
                var objectType = parentIsArray ? SchemaTypes.ObjectArray : SchemaTypes.Object;
                error = Messages.PropertiesRequired(parentChain, objectType);
            }

            return false;
        }

        var siblingNames = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < declared.Count; index++)
        {
            if (!TryBuildProperty(declared[index], childDefinitionLevel, parentChain, index, siblingNames, context, out var node, out error))
            {
                properties = [];
                return false;
            }

            properties.Add(node);
        }

        error = string.Empty;
        return true;
    }

    private static bool TryBuildProperty(
        PropertyInfo property,
        int definitionLevel,
        string parentChain,
        int index,
        HashSet<string> siblingNames,
        NullabilityInfoContext context,
        out JsonObject node,
        out string error)
    {
        node = [];
        var jsonName = GetJsonName(property);
        var nameIsValid = SchemaNames.IsValid(jsonName);
        var element = nameIsValid
            ? jsonName
            : "properties[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        var chain = SchemaPaths.Chain(parentChain, element);

        if (definitionLevel >= FirstDisallowedDefinitionLevel)
        {
            error = Messages.Problem(chain, Messages.NestingTooDeep);
            return false;
        }

        if (!nameIsValid)
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "name"), Messages.InvalidName);
            return false;
        }

        if (!siblingNames.Add(jsonName))
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "name"), Messages.DuplicateName);
            return false;
        }

        if (!TryMap(property.PropertyType, out var mapped))
        {
            error = Messages.Problem(chain, Messages.UnsupportedMemberType);
            return false;
        }

        IReadOnlyList<string>? enumValues = null;
        if (mapped.Kind == MappedKind.Enum)
        {
            if (!TryGetEnumNames(mapped.ClrType!, out var names, out var enumProblem))
            {
                var problem = enumProblem == EnumNameProblem.Duplicate
                    ? Messages.DuplicateEnumValue
                    : Messages.EnumMustNotBeEmpty;
                error = Messages.Problem(SchemaPaths.Field(chain, "enum"), problem);
                return false;
            }

            enumValues = names;
        }

        JsonArray? children = null;
        if (mapped.Kind == MappedKind.Object)
        {
            if (!TryBuildProperties(mapped.ClrType!, definitionLevel + 1, chain, mapped.IsArray, context, out var built, out error))
            {
                return false;
            }

            children = built;
        }

        node = new JsonObject
        {
            ["name"] = jsonName,
            ["type"] = mapped.SchemaType,
        };

        if (IsRequired(property, context))
        {
            node["required"] = true;
        }

        if (enumValues is not null)
        {
            var enumArray = new JsonArray();
            foreach (var value in enumValues)
            {
                enumArray.Add(value);
            }

            node["enum"] = enumArray;
        }

        if (children is not null)
        {
            node["properties"] = children;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryMap(Type propertyType, out MappedType mapped)
    {
        var effective = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (TryGetCollectionElement(effective, out var element))
        {
            var item = Nullable.GetUnderlyingType(element) ?? element;
            return TryMapCore(item, isArray: true, out mapped);
        }

        return TryMapCore(effective, isArray: false, out mapped);
    }

    private static bool TryMapCore(Type type, bool isArray, out MappedType mapped)
    {
        if (type.IsEnum)
        {
            mapped = new MappedType(MappedKind.Enum, isArray ? SchemaTypes.StringArray : SchemaTypes.String, isArray, type);
            return true;
        }

        var scalar = ScalarName(type);
        if (scalar is not null)
        {
            mapped = new MappedType(MappedKind.Scalar, isArray ? scalar + "[]" : scalar, isArray, null);
            return true;
        }

        if (IsDataClass(type))
        {
            mapped = new MappedType(MappedKind.Object, isArray ? SchemaTypes.ObjectArray : SchemaTypes.Object, isArray, type);
            return true;
        }

        mapped = default;
        return false;
    }

    private static string? ScalarName(Type type)
    {
        if (type == typeof(bool))
        {
            return SchemaTypes.Boolean;
        }

        if (type == typeof(byte))
        {
            return SchemaTypes.Byte;
        }

        if (type == typeof(short))
        {
            return SchemaTypes.Int16;
        }

        if (type == typeof(int))
        {
            return SchemaTypes.Int32;
        }

        if (type == typeof(long))
        {
            return SchemaTypes.Int64;
        }

        if (type == typeof(float))
        {
            return SchemaTypes.Single;
        }

        if (type == typeof(double))
        {
            return SchemaTypes.Double;
        }

        if (type == typeof(decimal))
        {
            return SchemaTypes.Decimal;
        }

        if (type == typeof(DateTime))
        {
            return SchemaTypes.DateTime;
        }

        if (type == typeof(string))
        {
            return SchemaTypes.String;
        }

        return null;
    }

    // Section 5.2: Object is a concrete user class. Abstract classes, interfaces, delegates,
    // and classes in the System or Microsoft namespaces are unsupported.
    private static bool IsDataClass(Type type)
    {
        if (!type.IsClass || type.IsArray || type == typeof(object))
        {
            return false;
        }

        if (type.IsAbstract || type.IsInterface || typeof(Delegate).IsAssignableFrom(type))
        {
            return false;
        }

        if (type.ContainsGenericParameters)
        {
            return false;
        }

        var typeNamespace = type.Namespace;
        if (typeNamespace is not null
            && (typeNamespace == "System"
                || typeNamespace.StartsWith("System.", StringComparison.Ordinal)
                || typeNamespace == "Microsoft"
                || typeNamespace.StartsWith("Microsoft.", StringComparison.Ordinal)))
        {
            return false;
        }

        return true;
    }

    private static bool TryGetCollectionElement(Type type, out Type element)
    {
        if (type.IsArray && type.GetArrayRank() == 1)
        {
            var item = type.GetElementType()!;
            if (item == typeof(byte))
            {
                element = type;
                return false;
            }

            element = item;
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            element = type.GetGenericArguments()[0];
            return true;
        }

        element = type;
        return false;
    }

    private static bool TryGetEnumNames(Type enumType, out List<string> names, out EnumNameProblem problem)
    {
        names = [];
        problem = EnumNameProblem.None;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
            var name = attribute?.Name ?? field.Name;
            if (!seen.Add(name))
            {
                problem = EnumNameProblem.Duplicate;
                return false;
            }

            names.Add(name);
        }

        if (names.Count == 0)
        {
            problem = EnumNameProblem.Empty;
            return false;
        }

        return true;
    }

    private static bool IsIgnoredWhenReading(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<JsonIgnoreAttribute>();
        if (attribute is null)
        {
            return false;
        }

        return attribute.Condition is JsonIgnoreCondition.Always or JsonIgnoreCondition.WhenReading;
    }

    private static string GetJsonName(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        return attribute?.Name ?? property.Name;
    }

    private static bool IsRequired(PropertyInfo property, NullabilityInfoContext context)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
        {
            return false;
        }

        if (property.PropertyType.IsValueType)
        {
            return true;
        }

        var info = context.Create(property);
        return info.ReadState != NullabilityState.Nullable && info.WriteState != NullabilityState.Nullable;
    }

    private static List<PropertyInfo> GetProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0
                && property.GetMethod is { IsPublic: true }
                && !IsIgnoredWhenReading(property))
            .OrderBy(property => property.DeclaringType == type ? 1 : 0)
            .ThenBy(property => property.MetadataToken)
            .ToList();
    }

    private enum MappedKind
    {
        Scalar,
        Enum,
        Object,
    }

    private enum EnumNameProblem
    {
        None,
        Empty,
        Duplicate,
    }

    private readonly struct MappedType
    {
        internal MappedType(MappedKind kind, string schemaType, bool isArray, Type? clrType)
        {
            Kind = kind;
            SchemaType = schemaType;
            IsArray = isArray;
            ClrType = clrType;
        }

        internal MappedKind Kind { get; }

        internal string SchemaType { get; }

        internal bool IsArray { get; }

        internal Type? ClrType { get; }
    }

    #endregion
}
