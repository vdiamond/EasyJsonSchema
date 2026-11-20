namespace EasyJsonSchema;

/// <summary>
/// Reads a <see cref="DateTime"/> as UTC when the text has a zone, and leaves a zoneless value unspecified.
/// The built-in converter stores offsets as local time.
/// </summary>
internal sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("The JSON value is not a date-time.");
        }

        // A value with no zone is not an offset. TryGetDateTimeOffset would treat it as local
        // time and reject the DateTime extremes on one side of UTC.
        if (HasZone(reader.ValueSpan))
        {
            if (!reader.TryGetDateTimeOffset(out var offset))
            {
                throw new JsonException("The JSON value is not a date-time.");
            }

            return DateTime.SpecifyKind(offset.UtcDateTime, DateTimeKind.Utc);
        }

        if (!reader.TryGetDateTime(out var value))
        {
            throw new JsonException("The JSON value is not a date-time.");
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture));
            return;
        }

        if (value.Kind == DateTimeKind.Local)
        {
            var utc = value.ToUniversalTime();
            writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture));
            return;
        }

        writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture));
    }

    #region private

    private static bool HasZone(ReadOnlySpan<byte> text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        if (text[^1] is (byte)'Z' or (byte)'z')
        {
            return true;
        }

        if (text.IndexOf((byte)'+') >= 0)
        {
            return true;
        }

        var timeIndex = text.IndexOf((byte)'T');
        if (timeIndex < 0)
        {
            timeIndex = text.IndexOf((byte)'t');
        }

        if (timeIndex >= 0 && text[(timeIndex + 1)..].IndexOf((byte)'-') >= 0)
        {
            return true;
        }

        return false;
    }

    #endregion
}
