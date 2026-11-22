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

    private const int MaximumPathSegments = 12;

    [ThreadStatic]
    private static PathSegment[]? t_pathSegments;

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

        if (!ReadObject(ref reader, schema.Properties, level: 0, DataPath.Root(utf8), out error))
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
        DataPath path,
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
                    error = Messages.Problem(path.Format(), Messages.EmptyObjectNotAllowed);
                    return false;
                }

                for (var index = 0; index < properties.Length; index++)
                {
                    if (properties[index].Required && !seen[index])
                    {
                        error = Messages.Problem(path.FormatProperty(properties[index].Name), Messages.Required);
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
            // Compared after unescaping. The path keeps the name as it was written, escapes included.
            var nameToken = reader.TokenStartIndex;
            var match = FindProperty(ref reader, properties);
            if (match >= 0 && seen[match])
            {
                error = Messages.Problem(path.FormatWrittenProperty(nameToken), Messages.DuplicateProperty);
                return false;
            }

            if (match < 0)
            {
                error = Messages.Problem(path.FormatWrittenProperty(nameToken), Messages.AdditionalPropertiesNotAllowed);
                return false;
            }

            seen[match] = true;
            if (!reader.Read())
            {
                error = Messages.MalformedJson;
                return false;
            }

            var property = properties[match];
            if (!ValidateValue(ref reader, property, level, path, nameToken, out error))
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
        DataPath path,
        long nameToken,
        out string error)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            if (property.Required)
            {
                error = Messages.Problem(path.FormatWrittenProperty(nameToken), Messages.NullNotAllowed);
                return false;
            }

            error = string.Empty;
            return true;
        }

        // An object at level 3 fails before the type check, including when the schema expected another type.
        if (reader.TokenType == JsonTokenType.StartObject && level >= MaximumObjectLevel)
        {
            error = Messages.Problem(path.FormatWrittenProperty(nameToken), Messages.NestingTooDeep);
            return false;
        }

        switch (property.Kind)
        {
            case SchemaValueKind.Array:
            {
                return ValidateArray(ref reader, property, level, path.ChildProperty(nameToken), out error);
            }
            case SchemaValueKind.Object:
            {
                return ValidateObjectValue(ref reader, property, level, path.ChildProperty(nameToken), out error);
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
                    new ScalarLocation(path, nameToken),
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
        DataPath path,
        out string error)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            error = Messages.Expected(path.Format(), SchemaTypes.Object, JsonKinds.Of(reader.TokenType));
            return false;
        }

        return ReadObject(ref reader, property.Children ?? [], level + 1, path, out error);
    }

    private static bool ValidateArray(
        ref Utf8JsonReader reader,
        CompiledProperty property,
        int level,
        DataPath path,
        out string error)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            error = Messages.Expected(path.Format(), property.TypeName, JsonKinds.Of(reader.TokenType));
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
                error = Messages.Problem(path.FormatIndex(index), Messages.NullNotAllowed);
                return false;
            }

            if (reader.TokenType == JsonTokenType.StartObject && level >= MaximumObjectLevel)
            {
                error = Messages.Problem(path.FormatIndex(index), Messages.NestingTooDeep);
                return false;
            }

            if (property.ItemKind == SchemaValueKind.Object)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    error = Messages.Expected(path.FormatIndex(index), SchemaTypes.Object, JsonKinds.Of(reader.TokenType));
                    return false;
                }

                if (!ReadObject(ref reader, property.Children ?? [], level + 1, path.ChildIndex(index), out error))
                {
                    return false;
                }
            }
            else if (!ValidateScalar(
                ref reader,
                property.ItemKind,
                property.ItemTypeName,
                property.EnumUtf8,
                new ScalarLocation(path, index),
                out error))
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

    private readonly ref struct ScalarLocation
    {
        internal ScalarLocation(DataPath parent, long nameToken)
        {
            _parent = parent;
            _nameToken = nameToken;
            _index = 0;
            _isIndex = false;
        }

        internal ScalarLocation(DataPath parent, int index)
        {
            _parent = parent;
            _nameToken = 0;
            _index = index;
            _isIndex = true;
        }

        internal string Path()
        {
            if (_isIndex)
            {
                return _parent.FormatIndex(_index);
            }

            return _parent.FormatWrittenProperty(_nameToken);
        }

        #region private

        private readonly DataPath _parent;

        private readonly long _nameToken;

        private readonly int _index;

        private readonly bool _isIndex;

        #endregion
    }

    private readonly struct PathSegment
    {
        internal PathSegment(long nameToken, int index, bool isIndex)
        {
            NameToken = nameToken;
            Index = index;
            IsIndex = isIndex;
        }

        internal long NameToken { get; }

        internal int Index { get; }

        internal bool IsIndex { get; }
    }

    private readonly ref struct DataPath
    {
        internal static DataPath Root(ReadOnlySpan<byte> utf8)
        {
            var segments = t_pathSegments;
            if (segments is null)
            {
                segments = new PathSegment[MaximumPathSegments];
                t_pathSegments = segments;
            }

            return new DataPath(utf8, segments, 0);
        }

        internal DataPath ChildProperty(long nameToken)
        {
            return Push(new PathSegment(nameToken, 0, false));
        }

        internal DataPath ChildIndex(int index)
        {
            return Push(new PathSegment(0, index, true));
        }

        internal string Format()
        {
            if (_count == 0)
            {
                return "$";
            }

            return FormatRange(_count);
        }

        internal string FormatProperty(string name)
        {
            if (_count == 0)
            {
                return name;
            }

            return FormatRange(_count) + "." + name;
        }

        internal string FormatWrittenProperty(long nameToken)
        {
            var written = RawName(nameToken);
            if (_count == 0)
            {
                return written;
            }

            return FormatRange(_count) + "." + written;
        }

        internal string FormatIndex(int index)
        {
            return FormatRange(_count) + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        }

        #region private

        private readonly ReadOnlySpan<byte> _utf8;

        private readonly PathSegment[] _segments;

        private readonly int _count;

        private DataPath(ReadOnlySpan<byte> utf8, PathSegment[] segments, int count)
        {
            _utf8 = utf8;
            _segments = segments;
            _count = count;
        }

        private DataPath Push(PathSegment segment)
        {
            _segments[_count] = segment;
            return new DataPath(_utf8, _segments, _count + 1);
        }

        private string FormatRange(int count)
        {
            var builder = new StringBuilder();
            for (var index = 0; index < count; index++)
            {
                var segment = _segments[index];
                if (segment.IsIndex)
                {
                    builder.Append('[');
                    builder.Append(segment.Index.ToString(CultureInfo.InvariantCulture));
                    builder.Append(']');
                }
                else
                {
                    if (index > 0)
                    {
                        builder.Append('.');
                    }

                    builder.Append(RawName(segment.NameToken));
                }
            }

            return builder.ToString();
        }

        private string RawName(long tokenStart)
        {
            var start = (int)tokenStart;
            if ((uint)start >= (uint)_utf8.Length || _utf8[start] != (byte)'"')
            {
                return string.Empty;
            }

            var index = start + 1;
            while (index < _utf8.Length)
            {
                var current = _utf8[index];
                if (current == (byte)'\\')
                {
                    if (index + 1 < _utf8.Length && _utf8[index + 1] == (byte)'u')
                    {
                        index += 6;
                    }
                    else
                    {
                        index += 2;
                    }

                    continue;
                }

                if (current == (byte)'"')
                {
                    return Encoding.UTF8.GetString(_utf8.Slice(start + 1, index - start - 1));
                }

                index++;
            }

            return Encoding.UTF8.GetString(_utf8[(start + 1)..]);
        }

        #endregion
    }

    #endregion
}
