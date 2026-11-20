namespace EasyJsonSchema.Tests;

internal static class Check
{
    internal static SchemaService Service { get; } = new();

    internal static SchemaDocument Load(string schemaJson)
    {
        Assert.True(Service.ValidateEasyJsonSchema(schemaJson, out var validationError), validationError);
        Assert.Equal(string.Empty, validationError);
        Assert.True(Service.DeserializeEasyJsonSchema(schemaJson, out var schema, out var readError), readError);
        Assert.NotNull(schema);
        return schema;
    }

    internal static void Data(SchemaDocument schema, string json, bool expectedOk, string expectedError = "")
    {
        var ok = Service.ValidateJsonObject(schema, json, out var error);
        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedOk ? string.Empty : expectedError, error);

        var bytes = Encoding.UTF8.GetBytes(json);
        var okBytes = Service.ValidateJsonObject(schema, bytes, out var errorBytes);
        Assert.Equal(ok, okBytes);
        Assert.Equal(error, errorBytes);
    }

    internal static void Schema(string schemaJson, bool expectedOk, string expectedError = "")
    {
        var ok = Service.ValidateEasyJsonSchema(schemaJson, out var error);
        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedOk ? string.Empty : expectedError, error);
    }
}
