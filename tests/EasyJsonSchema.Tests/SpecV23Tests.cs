using System.Globalization;
using System.Text.Json.Serialization;
using Fr19Status = EasyJsonSchema.Tests.Models.Fr19.Status;
using Fr19User = EasyJsonSchema.Tests.Models.Fr19.User;

namespace EasyJsonSchema.Tests;

public sealed class SpecV23Tests
{
    private const string ItemCompact = """{"properties":[{"name":"Id","type":"Int32","required":true},{"name":"Tags","type":"String[]","required":true}]}""";

    private const string ItemIndented = "{\n  \"properties\": [\n    {\n      \"name\": \"Id\",\n      \"type\": \"Int32\",\n      \"required\": true\n    },\n    {\n      \"name\": \"Tags\",\n      \"type\": \"String[]\",\n      \"required\": true\n    }\n  ]\n}";

    private const string Fr19Schema = """{"properties":[{"name":"Email","type":"String","required":true},{"name":"Status","type":"String","required":true,"enum":["Active","Pending","Disabled"]},{"name":"PreviousStatus","type":"String","enum":["Active","Pending","Disabled"]},{"name":"History","type":"String[]","required":true,"enum":["Active","Pending","Disabled"]}]}""";

    [Fact]
    public void T111_WriteIndented_ChangesOnlySchemaWhitespace()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<Item>(out var omitted), omitted);
        Assert.True(Check.Service.GenerateEasyJsonSchema<Item>(out var compact, false), compact);
        Assert.True(Check.Service.GenerateEasyJsonSchema<Item>(out var indented, true), indented);
        Assert.Equal(ItemCompact, omitted);
        Assert.Equal(ItemCompact, compact);
        Assert.Equal(ItemIndented, indented);

        var fromOmitted = LoadGenerated(omitted);
        var fromCompact = LoadGenerated(compact);
        var fromIndented = LoadGenerated(indented);
        AssertSameSchema(fromOmitted, fromCompact);
        AssertSameSchema(fromOmitted, fromIndented);
    }

    [Fact]
    public void T112_IndentedTypeGenerate_MatchesCompactSchema()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema(typeof(Fr19User), out var indented, true), indented);
        Assert.DoesNotContain("\r", indented);
        Assert.True(Check.Service.GenerateEasyJsonSchema<Fr19User>(out var compact), compact);
        Assert.Equal(Fr19Schema, compact);

        Assert.True(Check.Service.DeserializeEasyJsonSchema(indented, out var fromIndented, out var error), error);
        Assert.True(Check.Service.DeserializeEasyJsonSchema(compact, out var fromCompact, out error), error);
        Assert.NotNull(fromIndented);
        Assert.NotNull(fromCompact);
        AssertSameSchema(fromCompact, fromIndented);

        var schema = Check.Load(indented);
        Check.Data(schema, """{"Email":"a@b.com","Status":"Active","History":[]}""", true);
    }

    [Fact]
    public void T113_SerializeUser_RoundTripsThroughTheGeneratedSchema()
    {
        var user = new Fr19User
        {
            Email = "a@b.com",
            Status = Fr19Status.Active,
            PreviousStatus = null,
            History = [Fr19Status.Pending, Fr19Status.Active],
        };

        Assert.True(Check.Service.Serialize(user, out var json, out var error), error);
        Assert.Equal("""{"Email":"a@b.com","Status":"Active","PreviousStatus":null,"History":["Pending","Active"]}""", json);
        Assert.Equal(string.Empty, error);

        Assert.True(Check.Service.GenerateEasyJsonSchema<Fr19User>(out var schemaJson), schemaJson);
        Assert.Equal(Fr19Schema, schemaJson);
        Check.Data(Check.Load(schemaJson), json, true);

        Assert.True(Check.Service.Deserialize<Fr19User>(json, out var value, out error), error);
        Assert.NotNull(value);
        Assert.Equal("a@b.com", value.Email);
        Assert.Equal(Fr19Status.Active, value.Status);
        Assert.Null(value.PreviousStatus);
        Assert.Equal([Fr19Status.Pending, Fr19Status.Active], value.History);
    }

    [Fact]
    public void T114_SerializeEnum_UsesJsonMemberName()
    {
        var value = new ProgressHolder { Pr = NamedProgress.InProgress };
        Assert.True(Check.Service.Serialize(value, out var json, out var error), error);
        Assert.Equal("""{"Pr":"in-progress"}""", json);
        Assert.Equal(string.Empty, error);

        Assert.True(Check.Service.GenerateEasyJsonSchema<ProgressHolder>(out var schemaJson), schemaJson);
        Check.Data(Check.Load(schemaJson), json, true);
    }

    [Fact]
    public void T115_SerializeNumbers_UsesJsonSerializerFormat()
    {
        var sample = new Nums
        {
            B = 255,
            S = -1,
            I = 42,
            L = 9007199254740993,
            F = 1.5f,
            D = 0.1,
            M = 1.10m,
            Ok = true,
        };
        Assert.True(Check.Service.Serialize(sample, out var json, out var error), error);
        Assert.Equal("""{"B":255,"S":-1,"I":42,"L":9007199254740993,"F":1.5,"D":0.1,"M":1.10,"Ok":true}""", json);

        var extremes = new Nums
        {
            F = float.MaxValue,
            D = double.MaxValue,
            M = decimal.MaxValue,
            L = long.MinValue,
        };
        Assert.True(Check.Service.Serialize(extremes, out var extremeJson, out error), error);
        Assert.Equal("""{"B":0,"S":0,"I":0,"L":-9223372036854775808,"F":3.4028235E+38,"D":1.7976931348623157E+308,"M":79228162514264337593543950335,"Ok":false}""", extremeJson);

        Assert.True(Check.Service.GenerateEasyJsonSchema<Nums>(out var schemaJson), schemaJson);
        var schema = Check.Load(schemaJson);
        Check.Data(schema, json, true);
        Check.Data(schema, extremeJson, true);
    }

    [Fact]
    public void T116_SerializeDateTime_KeepsKindAndLocalOffset()
    {
        var utc = new DateTime(2026, 11, 19, 14, 22, 5, DateTimeKind.Utc);
        var unspecified = new DateTime(2026, 11, 19, 14, 22, 5, DateTimeKind.Unspecified);
        var milliseconds = new DateTime(2026, 11, 19, 14, 22, 5, 123, DateTimeKind.Utc);
        var local = new DateTime(2026, 11, 19, 14, 22, 5, DateTimeKind.Local);
        var value = new DateTimes
        {
            U = utc,
            N = unspecified,
            Ms = milliseconds,
            Lo = local,
        };

        var expected = "{\"U\":\"2026-11-19T14:22:05Z\",\"N\":\"2026-11-19T14:22:05\",\"Ms\":\"2026-11-19T14:22:05.123Z\",\"Lo\":\""
            + FormatLocal(local)
            + "\"}";
        Assert.True(Check.Service.Serialize(value, out var json, out var error), error);
        Assert.Equal(expected, json);
        Assert.Equal(string.Empty, error);

        Assert.True(Check.Service.GenerateEasyJsonSchema<DateTimes>(out var schemaJson), schemaJson);
        Check.Data(Check.Load(schemaJson), json, true);

        Assert.True(Check.Service.Deserialize<DateTimes>(json, out var roundTrip, out error), error);
        Assert.NotNull(roundTrip);
        Assert.Equal(utc, roundTrip.U);
        Assert.Equal(DateTimeKind.Utc, roundTrip.U.Kind);
        Assert.Equal(unspecified, roundTrip.N);
        Assert.Equal(DateTimeKind.Unspecified, roundTrip.N.Kind);
        Assert.Equal(milliseconds, roundTrip.Ms);
        Assert.Equal(DateTimeKind.Utc, roundTrip.Ms.Kind);
        Assert.Equal(local.ToUniversalTime(), roundTrip.Lo);
        Assert.Equal(DateTimeKind.Utc, roundTrip.Lo.Kind);
    }

    [Fact]
    public void T117_SerializeCollections_WritesArrays()
    {
        var filled = new Collections
        {
            A = [1, 2],
            D = [1, 2],
            Items = [new CityAddress { City = "Oslo" }],
            S = ["a"],
        };
        Assert.True(Check.Service.Serialize(filled, out var json, out var error), error);
        Assert.Equal("""{"A":[1,2],"D":[1,2],"Items":[{"City":"Oslo"}],"S":["a"]}""", json);

        var empty = new Collections
        {
            A = [],
            D = [],
            Items = [],
            S = [],
        };
        Assert.True(Check.Service.Serialize(empty, out var emptyJson, out error), error);
        Assert.Equal("""{"A":[],"D":[],"Items":[],"S":[]}""", emptyJson);

        Assert.True(Check.Service.GenerateEasyJsonSchema<Collections>(out var schemaJson), schemaJson);
        var schema = Check.Load(schemaJson);
        Check.Data(schema, json, true);
        Check.Data(schema, emptyJson, true);
    }

    [Fact]
    public void T118_SerializeIgnoreConditions_WritesIncludedProperties()
    {
        var value = new IgnoredMembers
        {
            Id = 0,
            Secret = "s",
            Hidden = "h",
            Note = null,
            Cnt = 0,
            Wr = "w",
        };
        Assert.True(Check.Service.Serialize(value, out var json, out var error), error);
        Assert.Equal("""{"Id":0,"Note":null,"Cnt":0,"Wr":"w"}""", json);
        Assert.Equal(string.Empty, error);

        Assert.True(Check.Service.GenerateEasyJsonSchema<IgnoredMembers>(out var schemaJson), schemaJson);
        Check.Data(Check.Load(schemaJson), json, true);
    }

    [Fact]
    public void T119_SerializeNames_EscapesTextAndPropertyNames()
    {
        var marked = new EscapedValue { Id = "<é>" };
        Assert.True(Check.Service.Serialize(marked, out var json, out var error), error);
        Assert.Equal("""{"id":"\u003C\u00E9\u003E"}""", json);
        Assert.True(Check.Service.GenerateEasyJsonSchema<EscapedValue>(out var schemaJson), schemaJson);
        Check.Data(Check.Load(schemaJson), json, true);

        var sized = new EscapedName { X = "x" };
        Assert.True(Check.Service.Serialize(sized, out var nameJson, out error), error);
        Assert.Equal("""{"Gr\u00F6\u00DFe":"x"}""", nameJson);
        Assert.True(Check.Service.GenerateEasyJsonSchema<EscapedName>(out var nameSchema), nameSchema);
        Check.Data(Check.Load(nameSchema), nameJson, true);
    }

    [Fact]
    public void T120_SerializeWriteIndented_UsesTheSchemaFormat()
    {
        var value = new Small { Id = 1, Tags = ["a"] };
        Assert.True(Check.Service.Serialize(value, out var compact, out var error, false), error);
        Assert.Equal("""{"Id":1,"Tags":["a"]}""", compact);
        Assert.True(Check.Service.Serialize(value, out var indented, out error, true), error);
        Assert.Equal("{\n  \"Id\": 1,\n  \"Tags\": [\n    \"a\"\n  ]\n}", indented);
    }

    [Fact]
    public void T121_SerializeFailures_ReturnDeveloperFaults()
    {
        Assert.False(Check.Service.Serialize<Fr19User>(null!, out var json, out var error));
        Assert.Equal("$: value is null", error);
        Assert.Equal(string.Empty, json);

        Assert.False(Check.Service.Serialize(new Nums { D = double.NaN }, out json, out error));
        Assert.Equal("$: cannot serialize Nums", error);
        Assert.Equal(string.Empty, json);

        Assert.False(Check.Service.Serialize(new Nums { F = float.PositiveInfinity }, out json, out error));
        Assert.Equal("$: cannot serialize Nums", error);
        Assert.Equal(string.Empty, json);

        var unnamed = new Fr19User
        {
            Email = "a@b.com",
            Status = (Fr19Status)99,
            History = [],
        };
        Assert.False(Check.Service.Serialize(unnamed, out json, out error));
        Assert.Equal("$: cannot serialize User", error);
        Assert.Equal(string.Empty, json);
    }

    [Fact]
    public void T122_WritingChanges_AreUnsupportedMemberTypes()
    {
        AssertUnsupported<WithFlags>("Fl");
        AssertUnsupported<WithNumberHandling>("N");
        AssertUnsupported<WithConverter>("Z");
        AssertUnsupported<WithPolymorphic>("Shape");
    }

    [Fact]
    public void T123_Serialize_RejectsOutputThatFailsTheSchema()
    {
        var user = new Fr19User
        {
            Email = null!,
            Status = Fr19Status.Active,
            History = [],
        };
        Assert.False(Check.Service.Serialize(user, out var json, out var error));
        Assert.Equal("Email: null not allowed", error);
        Assert.Equal(string.Empty, json);

        var small = new Small { Id = 1, Tags = [null!] };
        Assert.False(Check.Service.Serialize(small, out json, out error));
        Assert.Equal("Tags[0]: null not allowed", error);
        Assert.Equal(string.Empty, json);
    }

    [Fact]
    public void T124_CollectionClasses_AreUnsupported()
    {
        AssertUnsupported<WithTags>("Tags");
        AssertUnsupported<WithCounts>("Counts");

        Assert.False(Check.Service.Serialize(new WithTags { Tags = new Tags() }, out var json, out var error));
        Assert.Equal("Tags: unsupported member type", error);
        Assert.Equal(string.Empty, json);
    }

    [Fact]
    public void T125_Accessors_IncludeOnlyPublicGetAndSet()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<Accessors>(out var schemaJson), schemaJson);
        Assert.Equal("""{"properties":[{"name":"A","type":"Int32","required":true},{"name":"D","type":"Int32","required":true}]}""", schemaJson);
        var schema = Check.Load(schemaJson);
        Check.Data(schema, """{"A":1,"B":2}""", false, "B: additional properties not allowed");
        Check.Data(schema, """{"A":1,"D":4,"Ro":3}""", false, "Ro: additional properties not allowed");

        var value = new Accessors
        {
            A = 1,
            B = 2,
            C = 3,
            D = 4,
        };
        Assert.True(Check.Service.Serialize(value, out var json, out var error), error);
        Assert.Equal("""{"A":1,"D":4}""", json);

        Assert.True(Check.Service.Deserialize<Accessors>(json, out var roundTrip, out error), error);
        Assert.NotNull(roundTrip);
        Assert.Equal(1, roundTrip.A);
        Assert.Equal(4, roundTrip.D);
    }

    [Fact]
    public void T126_Serialize_ReturnsGenerationErrors()
    {
        Assert.False(Check.Service.Serialize(new WithGuid { G = Guid.NewGuid() }, out var json, out var error));
        Assert.Equal("G: unsupported member type", error);
        Assert.Equal(string.Empty, json);

        Assert.False(Check.Service.Serialize(new WithFlags { Fl = FlagSet.A }, out json, out error));
        Assert.Equal("Fl: unsupported member type", error);
        Assert.Equal(string.Empty, json);
    }

    [Fact]
    public void T127_EscapedKeys_MatchUnescapedAndKeepTheWrittenPath()
    {
        var schema = Check.Load("""{"properties":[{"name":"id","type":"Int32"}]}""");
        Check.Data(schema, """{"\u0069d":1}""", true);
        Check.Data(schema, """{"\u0078":1}""", false, """\u0078: additional properties not allowed""");
        Check.Data(schema, """{"id":1,"\u0069d":2}""", false, """\u0069d: duplicate property""");
    }

    #region private

    private static SchemaDocument LoadGenerated(string schemaJson)
    {
        Check.Schema(schemaJson, true);
        Assert.True(Check.Service.DeserializeEasyJsonSchema(schemaJson, out var schema, out var error), error);
        Assert.NotNull(schema);
        return schema;
    }

    private static void AssertUnsupported<T>(string propertyName)
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema<T>(out var error));
        Assert.Equal(propertyName + ": unsupported member type", error);
    }

    private static string FormatLocal(DateTime local)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        var sign = offset < TimeSpan.Zero ? "-" : "+";
        var absolute = offset.Duration();
        return "2026-11-19T14:22:05"
            + sign
            + absolute.Hours.ToString("00", CultureInfo.InvariantCulture)
            + ":"
            + absolute.Minutes.ToString("00", CultureInfo.InvariantCulture);
    }

    private static void AssertSameSchema(SchemaDocument expected, SchemaDocument actual)
    {
        Assert.Equal(expected.Title, actual.Title);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Meta, actual.Meta);
        AssertSameProperties(expected.Properties, actual.Properties);
    }

    private static void AssertSameProperties(IReadOnlyList<PropertyDef> expected, IReadOnlyList<PropertyDef> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (var index = 0; index < expected.Count; index++)
        {
            var left = expected[index];
            var right = actual[index];
            Assert.Equal(left.Name, right.Name);
            Assert.Equal(left.Type, right.Type);
            Assert.Equal(left.Required, right.Required);
            Assert.Equal(left.Title, right.Title);
            Assert.Equal(left.Description, right.Description);
            Assert.Equal(left.Meta, right.Meta);
            Assert.Equal(left.Enum, right.Enum);
            if (left.Properties is null)
            {
                Assert.Null(right.Properties);
                continue;
            }

            Assert.NotNull(right.Properties);
            AssertSameProperties(left.Properties, right.Properties);
        }
    }

    private sealed class Item
    {
        public int Id { get; set; }

        public List<string> Tags { get; set; } = new();
    }

    private enum NamedProgress
    {
        Todo,

        [JsonStringEnumMemberName("in-progress")]
        InProgress,
    }

    private sealed class ProgressHolder
    {
        public NamedProgress Pr { get; set; }
    }

    private sealed class Nums
    {
        public byte B { get; set; }

        public short S { get; set; }

        public int I { get; set; }

        public long L { get; set; }

        public float F { get; set; }

        public double D { get; set; }

        public decimal M { get; set; }

        public bool Ok { get; set; }
    }

    private sealed class DateTimes
    {
        public DateTime U { get; set; }

        public DateTime N { get; set; }

        public DateTime Ms { get; set; }

        public DateTime Lo { get; set; }
    }

    private sealed class CityAddress
    {
        public string City { get; set; } = "";
    }

    private sealed class Collections
    {
        public int[] A { get; set; } = [];

        public List<byte> D { get; set; } = new();

        public List<CityAddress> Items { get; set; } = new();

        public string[] S { get; set; } = [];
    }

    private sealed class IgnoredMembers
    {
        public int Id { get; set; }

        [JsonIgnore]
        public string? Secret { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenReading)]
        public string? Hidden { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Note { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Cnt { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWriting)]
        public string Wr { get; set; } = "";
    }

    private sealed class EscapedValue
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
    }

    private sealed class EscapedName
    {
        [JsonPropertyName("Größe")]
        public string X { get; set; } = "";
    }

    private sealed class Small
    {
        public int Id { get; set; }

        public List<string> Tags { get; set; } = new();
    }

    [Flags]
    private enum FlagSet
    {
        A = 1,
        B = 2,
    }

    private sealed class WithFlags
    {
        public FlagSet Fl { get; set; }
    }

    private sealed class WithNumberHandling
    {
        [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
        public int N { get; set; }
    }

    private sealed class WithConverter
    {
        [JsonConverter(typeof(MarkerConverter))]
        public string Z { get; set; } = "";
    }

    private sealed class MarkerConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    [JsonPolymorphic]
    [JsonDerivedType(typeof(Circle), "circle")]
    private class PolymorphicShape
    {
        public int Id { get; set; }
    }

    private sealed class Circle : PolymorphicShape
    {
        public int Radius { get; set; }
    }

    private sealed class WithPolymorphic
    {
        public PolymorphicShape Shape { get; set; } = new();
    }

    private sealed class Tags : List<string>
    {
    }

    private sealed class Counts : Dictionary<string, int>
    {
    }

    private sealed class WithTags
    {
        public Tags Tags { get; set; } = new();
    }

    private sealed class WithCounts
    {
        public Counts Counts { get; set; } = new();
    }

    private sealed class WithGuid
    {
        public Guid G { get; set; }
    }

    private sealed class Accessors
    {
        public int A { get; set; }

        public int B
        {
            set
            {
            }
        }

        public int C { private get; set; }

        public int D { get; init; }

        public int E { get; private set; }

        public int Ro => 3;
    }

    #endregion
}
