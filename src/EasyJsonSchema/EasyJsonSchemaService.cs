namespace EasyJsonSchema;

/// <summary>
/// Validates JSON objects, checks schema documents, deserializes stored JSON, generates schemas, and writes objects.
/// </summary>
public sealed class EasyJsonSchemaService : IEasyJsonSchema
{
    /// <inheritdoc />
    public bool ValidateJsonObject(EasyJsonSchema schema, string jsonObject, out string error)
    {
        if (schema is null)
        {
            error = Messages.SchemaIsNull;
            return false;
        }

        if (!JsonInput.TryPrepareString(jsonObject, out var prepared, out error))
        {
            return false;
        }

        var bytes = Encoding.UTF8.GetBytes(prepared);
        return ValidatePrepared(schema, bytes, out error);
    }

    /// <inheritdoc />
    public bool ValidateJsonObject(EasyJsonSchema schema, ReadOnlySpan<byte> utf8JsonObject, out string error)
    {
        if (schema is null)
        {
            error = Messages.SchemaIsNull;
            return false;
        }

        if (!JsonInput.TryPrepareBytes(utf8JsonObject, out var body, out error))
        {
            return false;
        }

        return ValidatePrepared(schema, body, out error);
    }

    /// <inheritdoc />
    public bool Deserialize<T>(string json, out T? value, out string error)
    {
        value = default;
        if (!JsonInput.TryPrepareString(json, out var prepared, out error))
        {
            return false;
        }

        var bytes = Encoding.UTF8.GetBytes(prepared);
        if (!JsonWellFormed.IsWellFormed(bytes))
        {
            error = Messages.MalformedJson;
            return false;
        }

        if (IsJsonNull(bytes))
        {
            error = Messages.CannotDeserialize(typeof(T).Name);
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(prepared, SerializerOptions.Data);
            error = string.Empty;
            return true;
        }
        catch (JsonException)
        {
            value = default;
            error = Messages.CannotDeserialize(typeof(T).Name);
            return false;
        }
        catch (NotSupportedException)
        {
            value = default;
            error = Messages.CannotDeserialize(typeof(T).Name);
            return false;
        }
        catch (InvalidOperationException)
        {
            value = default;
            error = Messages.CannotDeserialize(typeof(T).Name);
            return false;
        }
    }

    /// <inheritdoc />
    public bool ValidateEasyJsonSchema(string schemaJson, out string error)
    {
        if (!JsonInput.TryPrepareString(schemaJson, out var prepared, out error))
        {
            return false;
        }

        var bytes = Encoding.UTF8.GetBytes(prepared);
        return SchemaValidator.Validate(bytes, out error);
    }

    /// <inheritdoc />
    public bool DeserializeEasyJsonSchema(string schemaJson, out EasyJsonSchema? schema, out string error)
    {
        schema = null;
        if (!JsonInput.TryPrepareString(schemaJson, out var prepared, out error))
        {
            return false;
        }

        var bytes = Encoding.UTF8.GetBytes(prepared);
        if (!JsonWellFormed.IsWellFormed(bytes))
        {
            error = Messages.MalformedJson;
            return false;
        }

        if (IsJsonNull(bytes))
        {
            error = Messages.CannotDeserialize(nameof(EasyJsonSchema));
            return false;
        }

        try
        {
            schema = JsonSerializer.Deserialize<EasyJsonSchema>(prepared, SerializerOptions.Schema);
            error = string.Empty;
            return true;
        }
        catch (JsonException)
        {
            schema = null;
            error = Messages.CannotDeserialize(nameof(EasyJsonSchema));
            return false;
        }
        catch (NotSupportedException)
        {
            schema = null;
            error = Messages.CannotDeserialize(nameof(EasyJsonSchema));
            return false;
        }
        catch (InvalidOperationException)
        {
            schema = null;
            error = Messages.CannotDeserialize(nameof(EasyJsonSchema));
            return false;
        }
    }

    /// <inheritdoc />
    public bool GenerateEasyJsonSchema(Type type, out string errorOrSchema, bool writeIndented = false)
    {
        return SchemaGenerator.TryGenerate(type, writeIndented, out errorOrSchema);
    }

    /// <inheritdoc />
    public bool GenerateEasyJsonSchema<T>(out string errorOrSchema, bool writeIndented = false)
    {
        return GenerateEasyJsonSchema(typeof(T), out errorOrSchema, writeIndented);
    }

    /// <inheritdoc />
    public bool Serialize<T>(T value, out string json, out string error, bool writeIndented = false)
    {
        return ObjectSerializer.TrySerialize(value, writeIndented, out json, out error);
    }

    #region private

    private static bool ValidatePrepared(EasyJsonSchema schema, ReadOnlySpan<byte> utf8, out string error)
    {
        var compiled = SchemaCompiler.GetCompiled(schema);
        return DataValidator.Validate(compiled, utf8, out error);
    }

    private static bool IsJsonNull(ReadOnlySpan<byte> utf8)
    {
        var reader = new Utf8JsonReader(utf8);
        return reader.Read() && reader.TokenType == JsonTokenType.Null;
    }

    #endregion
}
