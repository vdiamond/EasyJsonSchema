namespace EasyJsonSchema;

internal static class DataPaths
{
    internal static string Property(string parent, string name)
    {
        if (parent.Length == 0)
        {
            return name;
        }

        return parent + "." + name;
    }

    internal static string Index(string parent, int index)
    {
        return parent + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
    }

    internal static string Object(string parent)
    {
        if (parent.Length == 0)
        {
            return "$";
        }

        return parent;
    }
}

internal static class SchemaPaths
{
    internal static string Chain(string parent, string element)
    {
        if (parent.Length == 0)
        {
            return element;
        }

        return parent + "." + element;
    }

    internal static string PropertiesItem(string parent, int index)
    {
        var element = "properties[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        return Chain(parent, element);
    }

    internal static string Field(string chain, string field)
    {
        return chain + "/" + field;
    }

    internal static string RootField(string field)
    {
        return "$/" + field;
    }

    internal static string EnumItem(string chain, int index)
    {
        return chain + "/enum[" + index.ToString(CultureInfo.InvariantCulture) + "]";
    }
}
