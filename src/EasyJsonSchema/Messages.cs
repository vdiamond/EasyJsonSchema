namespace EasyJsonSchema;

internal static class Messages
{
    internal const string NoJsonProvided = "$: no JSON provided";

    internal const string MalformedJson = "$: malformed JSON";

    internal const string SchemaIsNull = "$: schema is null";

    internal const string UnknownType = "unknown type (types are case-sensitive, e.g. Int32)";

    internal const string EnumOnlyOnString = "enum only allowed on String or String[]";

    internal const string EnumMustNotBeEmpty = "enum must not be empty";

    internal const string DuplicateEnumValue = "duplicate enum value";

    internal const string InvalidName = "invalid name";

    internal const string DuplicateName = "duplicate name";

    internal const string AtLeastOneProperty = "at least one property required";

    internal const string UnsupportedMemberType = "unsupported member type";

    internal const string IsoDateTime = "expected ISO-8601 date-time, e.g. 2026-11-19T14:22:05Z";

    internal const string Required = "required";

    internal const string NullNotAllowed = "null not allowed";

    internal const string EmptyObjectNotAllowed = "empty object not allowed";

    internal const string AdditionalPropertiesNotAllowed = "additional properties not allowed";

    internal const string DuplicateProperty = "duplicate property";

    internal const string NotInEnum = "not in enum";

    internal const string NestingTooDeep = "nesting too deep";

    internal static string CannotDeserialize(string typeName)
    {
        return "$: cannot deserialize to " + typeName;
    }

    internal static string Problem(string path, string problem)
    {
        return path + ": " + problem;
    }

    internal static string Expected(string path, string typeName, string kind)
    {
        return path + ": expected " + typeName + ", found " + kind;
    }

    internal static string OutOfRange(string path, string typeName)
    {
        return path + ": out of range for " + typeName;
    }

    internal static string DecimalOrExponent(string path, string typeName)
    {
        return path + ": decimal point or exponent not allowed for " + typeName;
    }

    internal static string PropertiesNotAllowed(string chain, string typeName)
    {
        return chain + "/properties: properties not allowed for " + typeName;
    }

    internal static string PropertiesRequired(string chain, string typeName)
    {
        return chain + "/properties: properties required for " + typeName;
    }
}
