namespace EasyJsonSchema;

internal static class SchemaValidator
{
    internal static bool Validate(ReadOnlySpan<byte> utf8, out string error)
    {
        if (!JsonWellFormed.IsWellFormed(utf8))
        {
            error = Messages.MalformedJson;
            return false;
        }

        try
        {
            var reader = new Utf8JsonReader(utf8);
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                if (!reader.TokenType.Equals(JsonTokenType.StartObject) && reader.TokenType != JsonTokenType.None)
                {
                    error = Messages.Expected("$", JsonKinds.Object, JsonKinds.Of(reader.TokenType));
                    return false;
                }

                error = Messages.MalformedJson;
                return false;
            }

            return ReadRoot(ref reader, out error);
        }
        catch (JsonException)
        {
            error = Messages.MalformedJson;
            return false;
        }
    }

    #region private

    // Definitions at level 4 fail. The root object is level 0 and is not a definition.
    private const int MaximumDefinitionLevel = 3;

    private static bool ReadRoot(ref Utf8JsonReader reader, out string error)
    {
        var keyCount = 0;
        var seen = new List<string>();
        var sawProperties = false;

        while (true)
        {
            if (!reader.Read())
            {
                error = Messages.MalformedJson;
                return false;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                error = Messages.MalformedJson;
                return false;
            }

            keyCount++;
            var field = reader.GetString() ?? string.Empty;
            if (ContainsOrdinal(seen, field))
            {
                error = Messages.Problem(SchemaPaths.RootField(field), Messages.DuplicateProperty);
                return false;
            }

            seen.Add(field);
            if (!IsRootField(field))
            {
                error = Messages.Problem(SchemaPaths.RootField(field), Messages.AdditionalPropertiesNotAllowed);
                return false;
            }

            if (!reader.Read())
            {
                error = Messages.MalformedJson;
                return false;
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                error = Messages.Problem(SchemaPaths.RootField(field), Messages.NullNotAllowed);
                return false;
            }

            switch (field)
            {
                case "title":
                case "description":
                case "$meta":
                {
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        error = Messages.Expected(SchemaPaths.RootField(field), SchemaTypes.String, JsonKinds.Of(reader.TokenType));
                        return false;
                    }

                    break;
                }
                case "properties":
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        error = Messages.Expected(SchemaPaths.RootField(field), SchemaTypes.ObjectArray, JsonKinds.Of(reader.TokenType));
                        return false;
                    }

                    if (!ReadPropertiesArray(ref reader, childLevel: 1, parentChain: string.Empty, isRoot: true, out _, out error))
                    {
                        return false;
                    }

                    sawProperties = true;
                    break;
                }
                default:
                {
                    error = Messages.MalformedJson;
                    return false;
                }
            }
        }

        if (keyCount == 0)
        {
            error = Messages.Problem("$", Messages.EmptyObjectNotAllowed);
            return false;
        }

        if (!sawProperties)
        {
            error = Messages.Problem(SchemaPaths.RootField("properties"), Messages.Required);
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool ReadPropertiesArray(
        ref Utf8JsonReader reader,
        int childLevel,
        string parentChain,
        bool isRoot,
        out int count,
        out string error)
    {
        count = 0;
        var siblings = new List<string>();
        while (true)
        {
            if (!reader.Read())
            {
                error = Messages.MalformedJson;
                return false;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
            {
                if (isRoot && count == 0)
                {
                    error = Messages.Problem("$", Messages.AtLeastOneProperty);
                    return false;
                }

                error = string.Empty;
                return true;
            }

            var itemPath = SchemaPaths.PropertiesItem(parentChain, count);
            if (reader.TokenType == JsonTokenType.Null)
            {
                error = Messages.Problem(itemPath, Messages.NullNotAllowed);
                return false;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                error = Messages.Expected(itemPath, JsonKinds.Object, JsonKinds.Of(reader.TokenType));
                return false;
            }

            if (childLevel > MaximumDefinitionLevel)
            {
                var chain = ResolveChain(reader, parentChain, count);
                error = Messages.Problem(chain, Messages.NestingTooDeep);
                return false;
            }

            if (!ReadDefinition(ref reader, childLevel, parentChain, count, siblings, out error))
            {
                return false;
            }

            count++;
        }
    }

    private static bool ReadDefinition(
        ref Utf8JsonReader reader,
        int level,
        string parentChain,
        int index,
        List<string> siblings,
        out string error)
    {
        var chain = ResolveChain(reader, parentChain, index);
        var keyCount = 0;
        var seen = new List<string>();
        string? nameValue = null;
        string? typeValue = null;
        var sawEnum = false;
        List<string>? enumValues = null;
        var sawProperties = false;
        var propertyCount = 0;

        while (true)
        {
            if (!reader.Read())
            {
                error = Messages.MalformedJson;
                return false;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                error = Messages.MalformedJson;
                return false;
            }

            keyCount++;
            var field = reader.GetString() ?? string.Empty;
            if (ContainsOrdinal(seen, field))
            {
                error = Messages.Problem(SchemaPaths.Field(chain, field), Messages.DuplicateProperty);
                return false;
            }

            seen.Add(field);
            if (!IsDefinitionField(field))
            {
                error = Messages.Problem(SchemaPaths.Field(chain, field), Messages.AdditionalPropertiesNotAllowed);
                return false;
            }

            if (!reader.Read())
            {
                error = Messages.MalformedJson;
                return false;
            }

            if (reader.TokenType == JsonTokenType.Null)
            {
                error = Messages.Problem(SchemaPaths.Field(chain, field), Messages.NullNotAllowed);
                return false;
            }

            switch (field)
            {
                case "name":
                {
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        error = Messages.Expected(SchemaPaths.Field(chain, field), SchemaTypes.String, JsonKinds.Of(reader.TokenType));
                        return false;
                    }

                    nameValue = reader.GetString() ?? string.Empty;
                    break;
                }
                case "type":
                {
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        error = Messages.Expected(SchemaPaths.Field(chain, field), SchemaTypes.String, JsonKinds.Of(reader.TokenType));
                        return false;
                    }

                    typeValue = reader.GetString() ?? string.Empty;
                    break;
                }
                case "required":
                {
                    if (reader.TokenType is not JsonTokenType.True and not JsonTokenType.False)
                    {
                        error = Messages.Expected(SchemaPaths.Field(chain, field), SchemaTypes.Boolean, JsonKinds.Of(reader.TokenType));
                        return false;
                    }

                    break;
                }
                case "title":
                case "description":
                case "$meta":
                {
                    if (reader.TokenType != JsonTokenType.String)
                    {
                        error = Messages.Expected(SchemaPaths.Field(chain, field), SchemaTypes.String, JsonKinds.Of(reader.TokenType));
                        return false;
                    }

                    break;
                }
                case "enum":
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        error = Messages.Expected(SchemaPaths.Field(chain, field), SchemaTypes.StringArray, JsonKinds.Of(reader.TokenType));
                        return false;
                    }

                    if (!ReadEnumArray(ref reader, chain, out enumValues, out error))
                    {
                        return false;
                    }

                    sawEnum = true;
                    break;
                }
                case "properties":
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        error = Messages.Expected(SchemaPaths.Field(chain, field), SchemaTypes.ObjectArray, JsonKinds.Of(reader.TokenType));
                        return false;
                    }

                    if (!ReadPropertiesArray(ref reader, level + 1, chain, isRoot: false, out propertyCount, out error))
                    {
                        return false;
                    }

                    sawProperties = true;
                    break;
                }
                default:
                {
                    error = Messages.MalformedJson;
                    return false;
                }
            }
        }

        if (keyCount == 0)
        {
            error = Messages.Problem(chain, Messages.EmptyObjectNotAllowed);
            return false;
        }

        if (nameValue is null)
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "name"), Messages.Required);
            return false;
        }

        if (typeValue is null)
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "type"), Messages.Required);
            return false;
        }

        if (!SchemaTypes.IsKnown(typeValue))
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "type"), Messages.UnknownType);
            return false;
        }

        if (sawEnum && !SchemaTypes.AllowsEnum(typeValue))
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "enum"), Messages.EnumOnlyOnString);
            return false;
        }

        if (sawEnum && enumValues!.Count == 0)
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "enum"), Messages.EnumMustNotBeEmpty);
            return false;
        }

        if (sawEnum && HasDuplicate(enumValues!))
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "enum"), Messages.DuplicateEnumValue);
            return false;
        }

        var isObject = SchemaTypes.IsObjectType(typeValue);
        if (sawProperties && !isObject)
        {
            error = Messages.PropertiesNotAllowed(chain, typeValue);
            return false;
        }

        if (isObject && (!sawProperties || propertyCount == 0))
        {
            error = Messages.PropertiesRequired(chain, typeValue);
            return false;
        }

        if (!SchemaNames.IsValid(nameValue))
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "name"), Messages.InvalidName);
            return false;
        }

        if (ContainsOrdinal(siblings, nameValue))
        {
            error = Messages.Problem(SchemaPaths.Field(chain, "name"), Messages.DuplicateName);
            return false;
        }

        siblings.Add(nameValue);
        error = string.Empty;
        return true;
    }

    private static bool ReadEnumArray(ref Utf8JsonReader reader, string chain, out List<string> values, out string error)
    {
        values = [];
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
                error = Messages.Problem(SchemaPaths.EnumItem(chain, index), Messages.NullNotAllowed);
                return false;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                error = Messages.Expected(SchemaPaths.EnumItem(chain, index), SchemaTypes.String, JsonKinds.Of(reader.TokenType));
                return false;
            }

            values.Add(reader.GetString() ?? string.Empty);
            index++;
        }
    }

    private static string ResolveChain(Utf8JsonReader reader, string parentChain, int index)
    {
        var name = LookAheadValidName(reader);
        if (name is null)
        {
            return SchemaPaths.PropertiesItem(parentChain, index);
        }

        return SchemaPaths.Chain(parentChain, name);
    }

    private static string? LookAheadValidName(Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            return null;
        }

        while (reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                return null;
            }

            var isName = reader.ValueTextEquals("name"u8);
            if (!reader.Read())
            {
                return null;
            }

            if (isName)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var value = reader.GetString();
                    if (value is not null && SchemaNames.IsValid(value))
                    {
                        return value;
                    }
                }

                return null;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }

            if (!reader.Read())
            {
                return null;
            }
        }

        return null;
    }

    private static bool IsRootField(string field)
    {
        return field is "title" or "description" or "$meta" or "properties";
    }

    private static bool IsDefinitionField(string field)
    {
        return field is "name" or "type" or "required" or "title" or "description" or "$meta" or "enum" or "properties";
    }

    private static bool ContainsOrdinal(List<string> values, string candidate)
    {
        foreach (var value in values)
        {
            if (string.Equals(value, candidate, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasDuplicate(List<string> values)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            if (!seen.Add(value))
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}
