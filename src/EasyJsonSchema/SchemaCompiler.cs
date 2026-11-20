using System.Runtime.CompilerServices;

namespace EasyJsonSchema;

internal static class SchemaCompiler
{
    internal static CompiledSchema GetCompiled(EasyJsonSchema schema)
    {
        return Cache.GetValue(schema, static value => Compile(value));
    }

    #region private

    // The public schema object stays unchanged. Compilation is stored beside it and
    // runs once per instance, including when several threads arrive together.
    private static readonly ConditionalWeakTable<EasyJsonSchema, CompiledSchema> Cache = new();

    private static CompiledSchema Compile(EasyJsonSchema schema)
    {
        var source = schema.Properties;
        var properties = new CompiledProperty[source.Count];
        for (var index = 0; index < source.Count; index++)
        {
            properties[index] = CompileProperty(source[index]);
        }

        return new CompiledSchema(properties);
    }

    private static CompiledProperty CompileProperty(PropertyDef property)
    {
        var name = property.Name ?? string.Empty;
        var typeName = property.Type ?? string.Empty;
        var kind = SchemaValueKind.Unknown;
        var itemKind = SchemaValueKind.Unknown;
        var itemTypeName = string.Empty;

        if (SchemaTypes.TryGetItemType(typeName, out var itemType) && SchemaTypes.IsKnownBase(itemType))
        {
            kind = SchemaValueKind.Array;
            itemKind = SchemaTypes.KindOfBase(itemType);
            itemTypeName = itemType;
        }
        else if (SchemaTypes.IsKnownBase(typeName))
        {
            kind = SchemaTypes.KindOfBase(typeName);
        }

        CompiledProperty[]? children = null;
        if (typeName is SchemaTypes.Object or SchemaTypes.ObjectArray)
        {
            var nested = property.Properties;
            if (nested is null)
            {
                children = [];
            }
            else
            {
                children = new CompiledProperty[nested.Count];
                for (var index = 0; index < nested.Count; index++)
                {
                    children[index] = CompileProperty(nested[index]);
                }
            }
        }

        byte[][]? enumUtf8 = null;
        if (property.Enum is not null)
        {
            enumUtf8 = new byte[property.Enum.Count][];
            for (var index = 0; index < property.Enum.Count; index++)
            {
                enumUtf8[index] = Encoding.UTF8.GetBytes(property.Enum[index]);
            }
        }

        return new CompiledProperty(
            name,
            Encoding.UTF8.GetBytes(name),
            property.Required,
            kind,
            typeName,
            itemKind,
            itemTypeName,
            enumUtf8,
            children);
    }

    #endregion
}
