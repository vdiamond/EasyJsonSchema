namespace EasyJsonSchema;

internal static class SchemaNames
{
    internal static bool IsValid(string name)
    {
        if (name.Length == 0)
        {
            return false;
        }

        foreach (var character in name)
        {
            if (character is '.' or '/' or '[' or ']')
            {
                return false;
            }
        }

        return true;
    }
}
