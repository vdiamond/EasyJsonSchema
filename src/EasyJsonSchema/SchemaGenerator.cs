using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;

namespace EasyJsonSchema;

internal static class SchemaGenerator
{
    internal static bool TryGenerate(Type type, bool writeIndented, out string errorOrSchema)
    {
        if (!TryCreate(type, out var schema, out var error) || schema is null)
        {
            errorOrSchema = error;
            return false;
        }

        errorOrSchema = WriteSchema(schema, writeIndented);
        return true;
    }

    internal static bool TryCreate(Type type, out EasyJsonSchema? schema, out string error)
    {
        schema = null;
        if (type is null)
        {
            error = Messages.TypeIsNull;
            return false;
        }

        var context = new NullabilityInfoContext();
        if (!TryBuildProperties(
            type,
            childDefinitionLevel: 1,
            parentChain: string.Empty,
            parentIsArray: false,
            context,
            out var properties,
            out error))
        {
            return false;
        }

        schema = new EasyJsonSchema
        {
            Properties = properties,
        };
        error = string.Empty;
        return true;
    }

    #region private

    private const int FirstDisallowedDefinitionLevel = 4;

    private const string SystemNamespace = "System";

    private const string SystemNamespacePrefix = "System.";

    private const string MicrosoftNamespace = "Microsoft";

    private const string MicrosoftNamespacePrefix = "Microsoft.";

    private static string WriteSchema(EasyJsonSchema schema, bool writeIndented)
    {
        var root = new JsonObject
        {
            ["properties"] = WriteProperties(schema.Properties),
        };
        var options = writeIndented ? SerializerOptions.Indented : SerializerOptions.Compact;
        return root.ToJsonString(options);
    }

    private static JsonArray WriteProperties(List<PropertyDef> properties)
    {
        var array = new JsonArray();
        foreach (var property in properties)
        {
            var node = new JsonObject
            {
                ["name"] = property.Name,
                ["type"] = property.Type,
            };

            if (property.Required)
            {
                node["required"] = true;
            }

            if (property.Enum is not null)
            {
                var enumArray = new JsonArray();
                foreach (var value in property.Enum)
                {
                    enumArray.Add(value);
                }

                node["enum"] = enumArray;
            }

            if (property.Properties is not null)
            {
                node["properties"] = WriteProperties(property.Properties);
            }

            array.Add(node);
        }

        return array;
    }

    private static bool TryBuildProperties(
        Type type,
        int childDefinitionLevel,
        string parentChain,
        bool parentIsArray,
        NullabilityInfoContext context,
        out List<PropertyDef> properties,
        out string error)
    {
        properties = [];
        var declared = MemberSelection.GetIncludedProperties(type);
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
            if (!TryBuildProperty(declared[index], childDefinitionLevel, parentChain, index, siblingNames, context, out var definition, out error))
            {
                properties = [];
                return false;
            }

            properties.Add(definition);
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
        out PropertyDef definition,
        out string error)
    {
        definition = new PropertyDef();
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

        if (ChangesWriting(property) || !TryMap(property.PropertyType, out var mapped))
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

        List<PropertyDef>? children = null;
        if (mapped.Kind == MappedKind.Object)
        {
            if (!TryBuildProperties(mapped.ClrType!, definitionLevel + 1, chain, mapped.IsArray, context, out var built, out error))
            {
                return false;
            }

            children = built;
        }

        definition = new PropertyDef
        {
            Name = jsonName,
            Type = mapped.SchemaType,
            Required = IsRequired(property, context),
            Enum = enumValues is null ? null : [.. enumValues],
            Properties = children,
        };

        error = string.Empty;
        return true;
    }

    private static bool ChangesWriting(PropertyInfo property)
    {
        if (property.GetCustomAttribute<JsonConverterAttribute>() is not null)
        {
            return true;
        }

        return property.GetCustomAttribute<JsonNumberHandlingAttribute>() is not null;
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
            if (type.GetCustomAttribute<FlagsAttribute>() is not null)
            {
                mapped = default;
                return false;
            }

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
            if (type.GetCustomAttribute<JsonPolymorphicAttribute>() is not null
                || type.GetCustomAttribute<JsonDerivedTypeAttribute>() is not null)
            {
                mapped = default;
                return false;
            }

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

    // Object is a concrete user class. Abstract classes, interfaces, delegates,
    // classes in the System or Microsoft namespaces, and collection classes are unsupported.
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
            && (typeNamespace == SystemNamespace
                || typeNamespace.StartsWith(SystemNamespacePrefix, StringComparison.Ordinal)
                || typeNamespace == MicrosoftNamespace
                || typeNamespace.StartsWith(MicrosoftNamespacePrefix, StringComparison.Ordinal)))
        {
            return false;
        }

        // JsonSerializer reads and writes a collection class as an array or a dictionary.
        if (typeof(IEnumerable).IsAssignableFrom(type))
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
