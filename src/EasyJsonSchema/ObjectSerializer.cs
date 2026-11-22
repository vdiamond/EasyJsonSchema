namespace EasyJsonSchema;

internal static class ObjectSerializer
{
    internal static bool TrySerialize<T>(T value, bool writeIndented, out string json, out string error)
    {
        json = string.Empty;
        if (value is null)
        {
            error = Messages.ValueIsNull;
            return false;
        }

        if (!GenerationCache.TryGet(typeof(T), out var schema, out error) || schema is null)
        {
            return false;
        }

        var options = writeIndented ? SerializerOptions.SerializeIndented : SerializerOptions.SerializeCompact;
        byte[] bytes;
        try
        {
            bytes = JsonSerializer.SerializeToUtf8Bytes(value, options);
        }
        catch (JsonException)
        {
            error = Messages.CannotSerialize(typeof(T).Name);
            return false;
        }
        catch (NotSupportedException)
        {
            error = Messages.CannotSerialize(typeof(T).Name);
            return false;
        }
        catch (InvalidOperationException)
        {
            error = Messages.CannotSerialize(typeof(T).Name);
            return false;
        }
        catch (ArgumentException)
        {
            error = Messages.CannotSerialize(typeof(T).Name);
            return false;
        }

        var compiled = SchemaCompiler.GetCompiled(schema);
        if (!DataValidator.Validate(compiled, bytes, out error))
        {
            return false;
        }

        json = Encoding.UTF8.GetString(bytes);
        error = string.Empty;
        return true;
    }
}
