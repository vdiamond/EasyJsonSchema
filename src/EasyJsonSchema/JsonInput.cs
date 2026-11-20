namespace EasyJsonSchema;

internal static class JsonInput
{
    internal static bool TryPrepareString(string? json, out string prepared, out string error)
    {
        if (json is null)
        {
            prepared = string.Empty;
            error = Messages.NoJsonProvided;
            return false;
        }

        if (json.Length > 0 && json[0] == '\uFEFF')
        {
            json = json[1..];
        }

        if (string.IsNullOrWhiteSpace(json))
        {
            prepared = string.Empty;
            error = Messages.NoJsonProvided;
            return false;
        }

        prepared = json;
        error = string.Empty;
        return true;
    }

    internal static bool TryPrepareBytes(ReadOnlySpan<byte> utf8, out ReadOnlySpan<byte> prepared, out string error)
    {
        if (HasBom(utf8))
        {
            utf8 = utf8[3..];
        }

        if (IsWhitespaceOnly(utf8))
        {
            prepared = default;
            error = Messages.NoJsonProvided;
            return false;
        }

        prepared = utf8;
        error = string.Empty;
        return true;
    }

    #region private

    private static bool HasBom(ReadOnlySpan<byte> utf8)
    {
        return utf8.Length >= 3
            && utf8[0] == 0xEF
            && utf8[1] == 0xBB
            && utf8[2] == 0xBF;
    }

    private static bool IsWhitespaceOnly(ReadOnlySpan<byte> utf8)
    {
        foreach (var value in utf8)
        {
            if (value > 0x7F)
            {
                return Encoding.UTF8.GetString(utf8).AsSpan().IsWhiteSpace();
            }

            if (!char.IsWhiteSpace((char)value))
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}
