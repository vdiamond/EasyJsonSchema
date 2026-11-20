using EasyJsonSchema.Tests.Models;
using EasyJsonSchema.Tests.Models.Enums;
using EasyJsonSchema.Tests.Models.Stored;
using StoredUser = EasyJsonSchema.Tests.Models.Stored.User;
using EnumUser = EasyJsonSchema.Tests.Models.Enums.User;

namespace EasyJsonSchema.Tests;

public sealed class DeserializeTests
{
    [Fact]
    public void T14_FractionalDateTime_ValidatesAndDeserializes()
    {
        var schema = Check.Load(Schemas.Created);
        const string json = """{"created":"2026-11-19T14:22:05.123Z"}""";
        Check.Data(schema, json, true);

        Assert.True(Check.Service.Deserialize<WithCreated>(json, out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal(string.Empty, error);
        Assert.Equal(DateTimeKind.Utc, value.Created.Kind);
        Assert.Equal(2026, value.Created.Year);
        Assert.Equal(11, value.Created.Month);
        Assert.Equal(19, value.Created.Day);
        Assert.Equal(14, value.Created.Hour);
        Assert.Equal(22, value.Created.Minute);
        Assert.Equal(5, value.Created.Second);
        Assert.Equal(123, value.Created.Millisecond);
    }

    [Fact]
    public void T23_NullValues_ValidateAndDeserialize()
    {
        Check.Data(Check.Load(Schemas.IdRequired), """{"id":null}""", false, "id: null not allowed");
        Check.Data(Check.Load(Schemas.IdAndAge), """{"id":1,"age":null}""", true);

        Assert.True(Check.Service.Deserialize<NullableAgeUser>("""{"id":1,"age":null}""", out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal(1, value.Id);
        Assert.Null(value.Age);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void T32_DateAndOffset_ValidateAndDeserializeToUtc()
    {
        var schema = Check.Load(Schemas.Created);
        const string dateOnly = """{"created":"2026-11-19"}""";
        const string offset = """{"created":"2026-11-19T14:22:05+01:00"}""";
        Check.Data(schema, dateOnly, true);
        Check.Data(schema, offset, true);

        Assert.True(Check.Service.Deserialize<WithCreated>(offset, out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal(DateTimeKind.Utc, value.Created.Kind);
        Assert.Equal(2026, value.Created.Year);
        Assert.Equal(11, value.Created.Month);
        Assert.Equal(19, value.Created.Day);
        Assert.Equal(13, value.Created.Hour);
        Assert.Equal(22, value.Created.Minute);
        Assert.Equal(5, value.Created.Second);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void T49_DateTimeZones_KeepTheSpecifiedKind()
    {
        var schema = Check.Load(Schemas.Created);
        AssertDate(schema, "2026-11-19T14:22", DateTimeKind.Unspecified, 14, 22, 0);
        AssertDate(schema, "2026-11-19T14:22:05Z", DateTimeKind.Utc, 14, 22, 5);
        AssertDate(schema, "2026-11-19T14:22:05+01:00", DateTimeKind.Utc, 13, 22, 5);
        AssertDate(schema, "2026-11-19T14:22:05", DateTimeKind.Unspecified, 14, 22, 5);
        AssertDate(schema, "2026-11-19", DateTimeKind.Unspecified, 0, 0, 0);
    }

    [Fact]
    public void T52_Deserialize_ReportsInputAndSerializerFaults()
    {
        Assert.True(Check.Service.Deserialize<StoredUser>("""{"Id":1,"Name":"a"}""", out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal(1, value.Id);
        Assert.Equal("a", value.Name);

        Assert.True(Check.Service.Deserialize<StoredUser>("\uFEFF{\"Id\":1,\"Name\":\"a\"}", out value, out error), error);
        Assert.NotNull(value);
        Assert.Equal(1, value.Id);

        AssertNoJson<StoredUser>(null);
        AssertNoJson<StoredUser>("");
        AssertNoJson<StoredUser>("   ");
        AssertNoJson<StoredUser>("\uFEFF");

        Assert.False(Check.Service.Deserialize<StoredUser>("""{"Id":""", out value, out error));
        Assert.Null(value);
        Assert.Equal("$: malformed JSON", error);

        Assert.False(Check.Service.Deserialize<StoredUser>("""{"Id":1} xyz""", out value, out error));
        Assert.Null(value);
        Assert.Equal("$: malformed JSON", error);

        Assert.False(Check.Service.Deserialize<StoredUser>("""{"Id":1,"Name":"a","nickname":"z"}""", out value, out error));
        Assert.Null(value);
        Assert.Equal("$: cannot deserialize to User", error);

        Assert.False(Check.Service.Deserialize<IntNameHolder>("""{"Name":"a"}""", out var intName, out error));
        Assert.Null(intName);
        Assert.Equal("$: cannot deserialize to " + typeof(IntNameHolder).Name, error);

        Assert.False(Check.Service.Deserialize<StreamHolder>("""{"Data":{"a":1}}""", out var streamValue, out error));
        Assert.Null(streamValue);
        Assert.Equal("$: cannot deserialize to " + typeof(StreamHolder).Name, error);
    }

    [Fact]
    public void T91_DeserializeEasyJsonSchema_DoesNotValidate()
    {
        Assert.True(Check.Service.DeserializeEasyJsonSchema(Schemas.FlatValid, out var schema, out var error), error);
        Assert.NotNull(schema);
        Assert.Equal("id", schema.Properties[0].Name);
        Assert.True(schema.Properties[0].Required);
        Assert.Equal(string.Empty, error);

        Assert.True(
            Check.Service.DeserializeEasyJsonSchema("""{"properties":[{"name":"n","type":"Int33"}]}""", out schema, out error),
            error);
        Assert.NotNull(schema);
        Assert.Equal("Int33", schema.Properties[0].Type);

        AssertSchemaNoJson(null);
        AssertSchemaNoJson("");
        AssertSchemaNoJson("   ");
        AssertSchemaNoJson("\uFEFF");

        Assert.False(Check.Service.DeserializeEasyJsonSchema("""{"properties":""", out schema, out error));
        Assert.Null(schema);
        Assert.Equal("$: malformed JSON", error);

        Assert.False(
            Check.Service.DeserializeEasyJsonSchema("""{"properties":[{"name":"a","type":"Int32","required":null}]}""", out schema, out error));
        Assert.Null(schema);
        Assert.Equal("$: cannot deserialize to EasyJsonSchema", error);

        Assert.False(
            Check.Service.DeserializeEasyJsonSchema("""{"properties":[{"name":"a","type":"Int32"}],"foo":1}""", out schema, out error));
        Assert.Null(schema);
        Assert.Equal("$: cannot deserialize to EasyJsonSchema", error);
    }

    [Fact]
    public void T96_Deserialize_DoesNotApplyASchema()
    {
        Assert.True(Check.Service.Deserialize<StoredUser>("""{"Id":1,"Name":"zz"}""", out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal("zz", value.Name);
        Assert.Equal(string.Empty, error);
    }

    [Fact]
    public void T97_EnumProperties_ReadNamesAndRejectNumbers()
    {
        Assert.True(Check.Service.Deserialize<EnumUser>("""{"Status":"Active"}""", out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal(Status.Active, value.Status);

        Assert.False(Check.Service.Deserialize<EnumUser>("""{"Status":1}""", out value, out error));
        Assert.Null(value);
        Assert.Equal("$: cannot deserialize to User", error);

        Assert.True(Check.Service.Deserialize<EnumUser>("""{"History":["Pending","Active"]}""", out value, out error), error);
        Assert.NotNull(value);
        Assert.Equal([Status.Pending, Status.Active], value.History);

        Assert.False(Check.Service.Deserialize<EnumUser>("""{"History":["Pending",1]}""", out value, out error));
        Assert.Null(value);
        Assert.Equal("$: cannot deserialize to User", error);
    }

    [Fact]
    public void T98_EnumNames_AreCaseInsensitiveOnDeserialize()
    {
        Assert.True(Check.Service.Deserialize<EnumUser>("""{"Status":"active"}""", out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal(Status.Active, value.Status);

        Assert.True(Check.Service.GenerateEasyJsonSchema<EnumUser>(out var schemaJson), schemaJson);
        var schema = Check.Load(schemaJson);
        Check.Data(schema, """{"Status":"active"}""", false, "Status: not in enum");
    }

    private static void AssertDate(SchemaDocument schema, string text, DateTimeKind kind, int hour, int minute, int second)
    {
        var json = "{\"created\":\"" + text + "\"}";
        Check.Data(schema, json, true);
        Assert.True(Check.Service.Deserialize<WithCreated>(json, out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal(string.Empty, error);
        Assert.Equal(kind, value.Created.Kind);
        Assert.Equal(2026, value.Created.Year);
        Assert.Equal(11, value.Created.Month);
        Assert.Equal(19, value.Created.Day);
        Assert.Equal(hour, value.Created.Hour);
        Assert.Equal(minute, value.Created.Minute);
        Assert.Equal(second, value.Created.Second);
    }

    private static void AssertNoJson<T>(string? json)
    {
        Assert.False(Check.Service.Deserialize<T>(json!, out T? value, out var error));
        Assert.Null(value);
        Assert.Equal("$: no JSON provided", error);
    }

    private static void AssertSchemaNoJson(string? json)
    {
        Assert.False(Check.Service.DeserializeEasyJsonSchema(json!, out var schema, out var error));
        Assert.Null(schema);
        Assert.Equal("$: no JSON provided", error);
    }
}
