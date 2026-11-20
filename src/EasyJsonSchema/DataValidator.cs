using System.Buffers.Text;

namespace EasyJsonSchema;

internal static class DataValidator
{
    internal static bool Validate(CompiledSchema schema, ReadOnlySpan<byte> utf8, out string error)
    {
        try
        {
            return ValidateCore(schema, utf8, out error);
        }
        catch (JsonException)
        {
            error = Messages.MalformedJson;
            return false;
        }
    }

    #region private

    // Objects at level 3 fail. Arrays do not add a level, so the reader depth is not used.
    private const int MaximumObjectLevel = 2;

    private static bool ValidateCore(CompiledSchema schema, ReadOnlySpan<byte> utf8, out string error)
    {
        var reader = new Utf8JsonReader(utf8);
        if (!reader.Read())
        {
            error = Messages.MalformedJson;
            return false;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            error = Messages.Expected("$", JsonKinds.Object, JsonKinds.Of(reader.TokenType));
            return false;
        }

        if (!ReadObject(ref reader, schema.Properties, level: 0, parentPath: string.Empty, out error))
        {
            return false;
        }

        if (reader.Read())
        {
            error = Messages.MalformedJson;
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool ReadObject(
        ref Utf8JsonReader reader,
        CompiledProperty[] properties,
        int level,
        string parentPath,
        out string error)
    {
        var keyCount = 0;
        Span<bool> seen = properties.Length <= 128
            ? stackalloc bool[properties.Length]
            : new bool[properties.Length];

        while (true)
        {
            if (!reader.Read())
            {
                error = Messages.MalformedJson;
                return false;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                if (keyCount == 0)
                {
                    error = Messages.Problem(DataPaths.Object(parentPath), Messages.EmptyObjectNotAllowed);
                    return false;
                }

                for (var index = 0; index < properties.Length; index++)
                {
                    if (properties[index].Required && !seen[index])
                    {
                        var requiredPath = DataPaths.Property(parentPath, properties[index].Name);
                        error = Messages.Problem(requiredPath, Messages.Required);
                        return false;
                    }
                }

                error = string.Empty;
                return true;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                error = Messages.MalformedJson;
                return false;
            }

            keyCount++;
            var match = FindProperty(ref reader, properties);
            if (match >= 0 && seen[match])
            {
                var duplicatePath = DataPaths.Property(parentPath, properties[match].Name);
                error = Messages.Problem(duplicatePath, Messages.DuplicateProperty);
                return false;
            }

            if (match < 0)
            {
                var dataName = reader.GetString() ?? string.Empty;
                var additionalPath = DataPaths.Property(parentPath, dataName);
                error = Messages.Problem(additionalPath, Messages.AdditionalPropertiesNotAllowed);
                return false;
            }

            seen[match] = true;
            if (!reader.Read())
            {
                error = Messages.MalformedJson;
                return false;
            }

            var property = properties[match];
            if (!ValidateValue(ref reader, property, level, parentPath, property.Name, out error))
            {
                return false;
            }
        }
    }

    private static int FindProperty(ref Utf8JsonReader reader, CompiledProperty[] properties)
    {
        for (var index = 0; index < properties.Length; index++)
        {
            if (reader.ValueTextEquals(properties[index].NameUtf8))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool ValidateValue(
        ref Utf8JsonReader reader,
        CompiledProperty property,
        int level,
        string parentPath,
        string name,
        out string error)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            if (property.Required)
            {
                error = Messages.Problem(DataPaths.Property(parentPath, name), Messages.NullNotAllowed);
                return false;
            }

            error = string.Empty;
            return true;
        }

        // An object at level 3 fails before the type check, including when the schema expected another type.
        if (reader.TokenType == JsonTokenType.StartObject && level >= MaximumObjectLevel)
        {
            error = Messages.Problem(DataPaths.Property(parentPath, name), Messages.NestingTooDeep);
            return false;
        }

        switch (property.Kind)
        {
            case SchemaValueKind.Array:
            {
                return ValidateArray(ref reader, property, level, DataPaths.Property(parentPath, name), out error);
            }
            case SchemaValueKind.Object:
            {
                return ValidateObjectValue(ref reader, property, level, DataPaths.Property(parentPath, name), out error);
            }
            case SchemaValueKind.Boolean:
            case SchemaValueKind.Integer:
            case SchemaValueKind.Float:
            case SchemaValueKind.DateTime:
            case SchemaValueKind.String:
            {
                return ValidateScalar(
                    ref reader,
                    property.Kind,
                    property.TypeName,
                    property.EnumUtf8,
                    parentPath,
                    name,
                    out error);
            }
            default:
            {
                throw new InvalidOperationException("The schema was not validated before use.");
            }
        }
    }

    private static bool ValidateObjectValue(
        ref Utf8JsonReader reader,
        CompiledProperty property,
        int level,
        string path,
        out string error)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            error = Messages.Expected(path, SchemaTypes.Object, JsonKinds.Of(reader.TokenType));
            return false;
        }

        return ReadObject(ref reader, property.Children ?? [], level + 1, path, out error);
    }

    private static bool ValidateArray(
        ref Utf8JsonReader reader,
        CompiledProperty property,
        int level,
        string path,
        out string error)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            error = Messages.Expected(path, property.TypeName, JsonKinds.Of(reader.TokenType));
            return false;
        }

        var index = 0;
        while (true)
        {
            if (!reader.Read())
            {
                error = Messages.MalformedJson;
                return false;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
            {
                error = string.Empty;
                return true;
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                error = Messages.Problem(DataPaths.Index(path, index), Messages.NullNotAllowed);
                return false;
            }

            if (reader.TokenType == JsonTokenType.StartObject && level >= MaximumObjectLevel)
            {
                error = Messages.Problem(DataPaths.Index(path, index), Messages.NestingTooDeep);
                return false;
            }

            if (property.ItemKind == SchemaValueKind.Object)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    error = Messages.Expected(DataPaths.Index(path, index), SchemaTypes.Object, JsonKinds.Of(reader.TokenType));
                    return false;
                }

                if (!ReadObject(ref reader, property.Children ?? [], level + 1, DataPaths.Index(path, index), out error))
                {
                    return false;
                }
            }
            else if (!ValidateScalar(ref reader, property.ItemKind, property.ItemTypeName, property.EnumUtf8, path, index, out error))
            {
                return false;
            }

            index++;
        }
    }

    private static bool ValidateScalar(
        ref Utf8JsonReader reader,
        SchemaValueKind kind,
        string typeName,
        byte[][]? enumUtf8,
        string parentPath,
        string name,
        out string error)
    {
        return ValidateScalar(ref reader, kind, typeName, enumUtf8, new ScalarLocation(parentPath, name), out error);
    }

    private static bool ValidateScalar(
        ref Utf8JsonReader reader,
        SchemaValueKind kind,
        string typeName,
        byte[][]? enumUtf8,
        string parentPath,
        int index,
        out string error)
    {
        return ValidateScalar(ref reader, kind, typeName, enumUtf8, new ScalarLocation(parentPath, index), out error);
    }

    private static bool ValidateScalar(
        ref Utf8JsonReader reader,
        SchemaValueKind kind,
        string typeName,
        byte[][]? enumUtf8,
        ScalarLocation location,
        out string error)
    {
        switch (kind)
        {
            case SchemaValueKind.Boolean:
            {
                if (reader.TokenType is not JsonTokenType.True and not JsonTokenType.False)
                {
                    error = Messages.Expected(location.Path(), typeName, JsonKinds.Of(reader.TokenType));
                    return false;
                }

                break;
            }
            case SchemaValueKind.Integer:
            {
                if (reader.TokenType != JsonTokenType.Number)
                {
                    error = Messages.Expected(location.Path(), typeName, JsonKinds.Of(reader.TokenType));
                    return false;
                }

                return ValidateInteger(reader.ValueSpan, typeName, location, out error);
            }
            case SchemaValueKind.Float:
            {
                if (reader.TokenType != JsonTokenType.Number)
                {
                    error = Messages.Expected(location.Path(), typeName, JsonKinds.Of(reader.TokenType));
                    return false;
                }

                return ValidateFloat(reader.ValueSpan, typeName, location, out error);
            }
            case SchemaValueKind.DateTime:
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    error = Messages.Expected(location.Path(), SchemaTypes.DateTime, JsonKinds.Of(reader.TokenType));
                    return false;
                }

                if (!reader.TryGetDateTime(out _))
                {
                    error = Messages.Problem(location.Path(), Messages.IsoDateTime);
                    return false;
                }

                break;
            }
            case SchemaValueKind.String:
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    error = Messages.Expected(location.Path(), typeName, JsonKinds.Of(reader.TokenType));
                    return false;
                }

                if (enumUtf8 is not null && !MatchesEnum(ref reader, enumUtf8))
                {
                    error = Messages.Problem(location.Path(), Messages.NotInEnum);
                    return false;
                }

                break;
            }
            default:
            {
                throw new InvalidOperationException("The schema was not validated before use.");
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool ValidateInteger(ReadOnlySpan<byte> literal, string typeName, ScalarLocation location, out string error)
    {
        if (ContainsDecimalOrExponent(literal))
        {
            error = Messages.DecimalOrExponent(location.Path(), typeName);
            return false;
        }

        if (!Utf8Parser.TryParse(literal, out long value, out var consumed) || consumed != literal.Length || !InIntegerRange(value, typeName))
        {
            error = Messages.OutOfRange(location.Path(), typeName);
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool ContainsDecimalOrExponent(ReadOnlySpan<byte> literal)
    {
        foreach (var value in literal)
        {
            if (value is (byte)'.' or (byte)'e' or (byte)'E')
            {
                return true;
            }
        }

        return false;
    }

    private static bool InIntegerRange(long value, string typeName)
    {
        return typeName switch
        {
            SchemaTypes.Byte => value is >= byte.MinValue and <= byte.MaxValue,
            SchemaTypes.Int16 => value is >= short.MinValue and <= short.MaxValue,
            SchemaTypes.Int32 => value is >= int.MinValue and <= int.MaxValue,
            SchemaTypes.Int64 => true,
            _ => throw new InvalidOperationException("The schema was not validated before use."),
        };
    }

    private static bool ValidateFloat(ReadOnlySpan<byte> literal, string typeName, ScalarLocation location, out string error)
    {
        if (!FloatLiteralAccepted(literal, typeName))
        {
            error = Messages.OutOfRange(location.Path(), typeName);
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool FloatLiteralAccepted(ReadOnlySpan<byte> literal, string typeName)
    {
        try
        {
            switch (typeName)
            {
                case SchemaTypes.Single:
                {
                    JsonSerializer.Deserialize<float>(literal);
                    return true;
                }
                case SchemaTypes.Double:
                {
                    JsonSerializer.Deserialize<double>(literal);
                    return true;
                }
                case SchemaTypes.Decimal:
                {
                    JsonSerializer.Deserialize<decimal>(literal);
                    return true;
                }
                default:
                {
                    throw new InvalidOperationException("The schema was not validated before use.");
                }
            }
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool MatchesEnum(ref Utf8JsonReader reader, byte[][] enumUtf8)
    {
        foreach (var value in enumUtf8)
        {
            if (reader.ValueTextEquals(value))
            {
                return true;
            }
        }

        return false;
    }

    private readonly struct ScalarLocation
    {
        internal ScalarLocation(string parent, string name)
        {
            _parent = parent;
            _name = name;
            _index = 0;
            _isIndex = false;
        }

        internal ScalarLocation(string parent, int index)
        {
            _parent = parent;
            _name = null;
            _index = index;
            _isIndex = true;
        }

        internal string Path()
        {
            if (_isIndex)
            {
                return DataPaths.Index(_parent, _index);
            }

            return DataPaths.Property(_parent, _name ?? string.Empty);
        }

        #region private

        private readonly string _parent;

        private readonly string? _name;

        private readonly int _index;

        private readonly bool _isIndex;

        #endregion
    }

    #endregion
}
