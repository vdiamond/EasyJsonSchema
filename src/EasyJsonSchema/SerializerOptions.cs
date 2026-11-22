using System.Text.Json.Serialization.Metadata;

namespace EasyJsonSchema;

internal static class SerializerOptions
{
    internal static JsonSerializerOptions Data { get; } = CreateData();

    internal static JsonSerializerOptions Schema { get; } = CreateSchema();

    internal static JsonSerializerOptions Compact { get; } = new()
    {
        WriteIndented = false,
    };

    internal static JsonSerializerOptions Indented { get; } = new()
    {
        WriteIndented = true,
        NewLine = "\n",
    };

    internal static JsonSerializerOptions SerializeCompact { get; } = CreateSerialize(writeIndented: false);

    internal static JsonSerializerOptions SerializeIndented { get; } = CreateSerialize(writeIndented: true);

    #region private

    private static JsonSerializerOptions CreateSerialize(bool writeIndented)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = writeIndented,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { MemberSelection.Apply },
            },
        };

        if (writeIndented)
        {
            options.NewLine = "\n";
        }

        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        return options;
    }

    private static JsonSerializerOptions CreateData()
    {
        var options = new JsonSerializerOptions
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new UtcDateTimeConverter());
        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        return options;
    }

    private static JsonSerializerOptions CreateSchema()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
    }

    #endregion
}
