namespace EasyJsonSchema;

internal static class SchemaTypes
{
    internal const string Boolean = "Boolean";

    internal const string Byte = "Byte";

    internal const string Int16 = "Int16";

    internal const string Int32 = "Int32";

    internal const string Int64 = "Int64";

    internal const string Single = "Single";

    internal const string Double = "Double";

    internal const string Decimal = "Decimal";

    internal const string DateTime = "DateTime";

    internal const string String = "String";

    internal const string Object = "Object";

    internal const string BooleanArray = "Boolean[]";

    internal const string ByteArray = "Byte[]";

    internal const string Int16Array = "Int16[]";

    internal const string Int32Array = "Int32[]";

    internal const string Int64Array = "Int64[]";

    internal const string SingleArray = "Single[]";

    internal const string DoubleArray = "Double[]";

    internal const string DecimalArray = "Decimal[]";

    internal const string DateTimeArray = "DateTime[]";

    internal const string StringArray = "String[]";

    internal const string ObjectArray = "Object[]";

    internal static bool IsKnown(string type)
    {
        if (IsKnownBase(type))
        {
            return true;
        }

        return TryGetItemType(type, out var itemType) && IsKnownBase(itemType);
    }

    internal static bool IsKnownBase(string type)
    {
        return type switch
        {
            Boolean or Byte or Int16 or Int32 or Int64 or Single or Double or Decimal or DateTime or String or Object => true,
            _ => false,
        };
    }

    internal static bool TryGetItemType(string type, out string itemType)
    {
        if (type.EndsWith("[]", StringComparison.Ordinal) && type.Length > 2)
        {
            itemType = type[..^2];
            return true;
        }

        itemType = "";
        return false;
    }

    internal static bool IsInteger(string type)
    {
        return type is Byte or Int16 or Int32 or Int64;
    }

    internal static bool IsFloat(string type)
    {
        return type is Single or Double or Decimal;
    }

    internal static bool AllowsEnum(string type)
    {
        return type is String or StringArray;
    }

    internal static bool IsObjectType(string type)
    {
        return type is Object or ObjectArray;
    }

    internal static SchemaValueKind KindOfBase(string type)
    {
        return type switch
        {
            Boolean => SchemaValueKind.Boolean,
            Byte or Int16 or Int32 or Int64 => SchemaValueKind.Integer,
            Single or Double or Decimal => SchemaValueKind.Float,
            DateTime => SchemaValueKind.DateTime,
            String => SchemaValueKind.String,
            Object => SchemaValueKind.Object,
            _ => SchemaValueKind.Unknown,
        };
    }
}
