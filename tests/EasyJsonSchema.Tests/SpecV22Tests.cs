using System.Text.Json.Serialization;

namespace EasyJsonSchema.Tests;

public sealed class SpecV22Tests
{
    [Fact]
    public void T103_ObjectSupport_AcceptsUserClassesAndRejectsTheRest()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<WithAddress>(out var schemaJson), schemaJson);
        var schema = Check.Load(schemaJson);
        Assert.Equal("Address", schema.Properties[0].Name);
        Assert.Equal("Object", schema.Properties[0].Type);
        var children = schema.Properties[0].Properties;
        Assert.NotNull(children);
        Assert.Equal("City", children[0].Name);
        Assert.Equal("String", children[0].Type);

        AssertUnsupported<WithShape>("Shape");
        AssertUnsupported<WithInterface>("IShape");
        AssertUnsupported<WithDelegate>("Callback");
        AssertUnsupported<WithStringBuilder>("Sb");
        AssertUnsupported<WithStream>("Data");
    }

    [Fact]
    public void T104_Arrays_MapExceptByteArray()
    {
        AssertArray<WithIntArray>("A", "Int32[]");
        AssertArray<WithStringArray>("B", "String[]");
        AssertArray<WithAddressArray>("C", "Object[]");
        AssertArray<WithByteList>("D", "Byte[]");
        AssertUnsupported<WithByteArray>("E");

        Assert.True(Check.Service.GenerateEasyJsonSchema<WithByteList>(out var schemaJson), schemaJson);
        var schema = Check.Load(schemaJson);
        Check.Data(schema, """{"D":[1,2]}""", true);
        Assert.True(Check.Service.Deserialize<WithByteList>("""{"D":[1,2]}""", out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal(new byte[] { 1, 2 }, value.D);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void T105_TypesOutsideTheTable_AreUnsupported()
    {
        AssertUnsupported<WithGuid>("Value");
        AssertUnsupported<WithIList>("Value");
        AssertUnsupported<WithJagged>("Value");
        AssertUnsupported<WithStruct>("Value");
        AssertUnsupported<WithDateTimeOffset>("Value");
        AssertUnsupported<WithTimeSpan>("Value");
        AssertUnsupported<WithDateOnly>("Value");
        AssertUnsupported<WithTimeOnly>("Value");
        AssertUnsupported<WithChar>("Value");
        AssertUnsupported<WithUri>("Value");
    }

    [Fact]
    public void T106_JsonIgnoreAndStaticMembers_AreSelectedByTheReadRules()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<MemberSelection>(out var schemaJson), schemaJson);
        var schema = Check.Load(schemaJson);
        Assert.Equal(2, schema.Properties.Count);
        Assert.Equal("Id", schema.Properties[0].Name);
        Assert.Equal("Int32", schema.Properties[0].Type);
        Assert.True(schema.Properties[0].Required);
        Assert.Equal("Note", schema.Properties[1].Name);
        Assert.Equal("String", schema.Properties[1].Type);
        Assert.False(schema.Properties[1].Required);
        Check.Data(schema, """{"Id":1,"Secret":"x"}""", false, "Secret: additional properties not allowed");
    }

    [Fact]
    public void T107_NullType_ReturnsTypeIsNull()
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema((Type)null!, out var error));
        Assert.Equal("$: type is null", error);
    }

    [Fact]
    public void T108_JsonNull_CannotDeserialize()
    {
        Assert.False(Check.Service.DeserializeEasyJsonSchema("null", out var schema, out var error));
        Assert.Null(schema);
        Assert.Equal("$: cannot deserialize to EasyJsonSchema", error);

        Assert.False(Check.Service.DeserializeEasyJsonSchema("\uFEFFnull", out schema, out error));
        Assert.Null(schema);
        Assert.Equal("$: cannot deserialize to EasyJsonSchema", error);

        Assert.False(Check.Service.Deserialize<Models.Stored.User>("null", out var value, out error));
        Assert.Null(value);
        Assert.Equal("$: cannot deserialize to User", error);
    }

    [Fact]
    public void T109_DuplicateJsonNames_MapInvalidOperationToCannotDeserialize()
    {
        Assert.False(Check.Service.Deserialize<Clash>("""{"A":1}""", out var value, out var error));
        Assert.Null(value);
        Assert.Equal("$: cannot deserialize to Clash", error);
    }

    [Fact]
    public void T110_Whitespace_IsJsonWhitespaceOnly()
    {
        var schema = Check.Load(Schemas.OptionalA);
        const string nbsp = "\u00A0";
        Check.Data(schema, nbsp, false, "$: malformed JSON");
        Assert.False(Check.Service.ValidateJsonObject(schema, new byte[] { 0xC2, 0xA0 }, out var error));
        Assert.Equal("$: malformed JSON", error);
        Check.Schema(nbsp, false, "$: malformed JSON");

        const string jsonWhitespace = " \t\r\n";
        Assert.False(Check.Service.ValidateJsonObject(schema, jsonWhitespace, out error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateJsonObject(schema, " \t\r\n"u8, out error));
        Assert.Equal("$: no JSON provided", error);
        Check.Schema(jsonWhitespace, false, "$: no JSON provided");
    }

    #region private

    private static void AssertArray<T>(string name, string schemaType)
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<T>(out var schemaJson), schemaJson);
        var schema = Check.Load(schemaJson);
        Assert.Equal(name, schema.Properties[0].Name);
        Assert.Equal(schemaType, schema.Properties[0].Type);
    }

    private static void AssertUnsupported<T>(string name)
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema<T>(out var error));
        Assert.Equal(name + ": unsupported member type", error);
    }

    private sealed class PostalAddress
    {
        public string City { get; set; } = "";
    }

    private sealed class WithAddress
    {
        public PostalAddress Address { get; set; } = new();
    }

    private abstract class Shape
    {
    }

    private sealed class WithShape
    {
        public Shape Shape { get; set; } = null!;
    }

    private interface IShape
    {
    }

    private sealed class WithInterface
    {
        public IShape IShape { get; set; } = null!;
    }

    private delegate void MyHandler();

    private sealed class WithDelegate
    {
        public MyHandler Callback { get; set; } = null!;
    }

    private sealed class WithStringBuilder
    {
        public StringBuilder Sb { get; set; } = new();
    }

    private sealed class WithStream
    {
        public Stream Data { get; set; } = Stream.Null;
    }

    private sealed class WithIntArray
    {
        public int[] A { get; set; } = [];
    }

    private sealed class WithStringArray
    {
        public string[] B { get; set; } = [];
    }

    private sealed class WithAddressArray
    {
        public PostalAddress[] C { get; set; } = [];
    }

    private sealed class WithByteList
    {
        public List<byte> D { get; set; } = new();
    }

    private sealed class WithByteArray
    {
        public byte[] E { get; set; } = [];
    }

    private sealed class WithGuid
    {
        public Guid Value { get; set; }
    }

    private sealed class WithIList
    {
        public IList<int> Value { get; set; } = new List<int>();
    }

    private sealed class WithJagged
    {
        public int[][] Value { get; set; } = [];
    }

    private struct CustomPoint
    {
        public int X { get; set; }
    }

    private sealed class WithStruct
    {
        public CustomPoint Value { get; set; }
    }

    private sealed class WithDateTimeOffset
    {
        public DateTimeOffset Value { get; set; }
    }

    private sealed class WithTimeSpan
    {
        public TimeSpan Value { get; set; }
    }

    private sealed class WithDateOnly
    {
        public DateOnly Value { get; set; }
    }

    private sealed class WithTimeOnly
    {
        public TimeOnly Value { get; set; }
    }

    private sealed class WithChar
    {
        public char Value { get; set; }
    }

    private sealed class WithUri
    {
        public Uri Value { get; set; } = new("https://example.com");
    }

    private sealed class MemberSelection
    {
        public int Id { get; set; }

        [JsonIgnore]
        public string Secret { get; set; } = "";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Note { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenReading)]
        public string? Hidden { get; set; }

        public static int Count { get; set; }
    }

    private sealed class Clash
    {
        public int A { get; set; }

        [JsonPropertyName("A")]
        public int B { get; set; }
    }

    #endregion
}
