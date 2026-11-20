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

        if (json.Length == 0 || IsJsonWhitespace(json.AsSpan()))
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

        if (IsJsonWhitespace(utf8))
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

    private static bool IsJsonWhitespace(ReadOnlySpan<char> text)
    {
        foreach (var value in text)
        {
            if (value is not (' ' or '\t' or '\n' or '\r'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsJsonWhitespace(ReadOnlySpan<byte> utf8)
    {
        foreach (var value in utf8)
        {
            if (value is not (0x20 or 0x09 or 0x0A or 0x0D))
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}
