namespace EasyJsonSchema;

internal static class JsonWellFormed
{
    internal static bool IsWellFormed(ReadOnlySpan<byte> utf8)
    {
        try
        {
            var reader = new Utf8JsonReader(utf8);
            if (!reader.Read())
            {
                return false;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }

            return !reader.Read();
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
