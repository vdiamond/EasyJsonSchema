namespace EasyJsonSchema;

internal sealed class CompiledSchema
{
    internal CompiledSchema(CompiledProperty[] properties)
    {
        Properties = properties;
    }

    internal CompiledProperty[] Properties { get; }
}

internal sealed class CompiledProperty
{
    internal CompiledProperty(
        string name,
        byte[] nameUtf8,
        bool required,
        SchemaValueKind kind,
        string typeName,
        SchemaValueKind itemKind,
        string itemTypeName,
        byte[][]? enumUtf8,
        CompiledProperty[]? children)
    {
        Name = name;
        NameUtf8 = nameUtf8;
        Required = required;
        Kind = kind;
        TypeName = typeName;
        ItemKind = itemKind;
        ItemTypeName = itemTypeName;
        EnumUtf8 = enumUtf8;
        Children = children;
    }

    internal string Name { get; }

    internal byte[] NameUtf8 { get; }

    internal bool Required { get; }

    internal SchemaValueKind Kind { get; }

    internal string TypeName { get; }

    internal SchemaValueKind ItemKind { get; }

    internal string ItemTypeName { get; }

    internal byte[][]? EnumUtf8 { get; }

    internal CompiledProperty[]? Children { get; }
}
