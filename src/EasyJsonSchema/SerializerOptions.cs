namespace EasyJsonSchema;

internal static class SerializerOptions
{
    internal static JsonSerializerOptions Data { get; } = CreateData();

    internal static JsonSerializerOptions Schema { get; } = CreateSchema();

    internal static JsonSerializerOptions Compact { get; } = new()
    {
        WriteIndented = false,
    };

    #region private

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
