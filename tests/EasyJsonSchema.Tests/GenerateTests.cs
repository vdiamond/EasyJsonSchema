using EasyJsonSchema.Tests.Models;
using EasyJsonSchema.Tests.Models.Fr19;
using Fr19User = EasyJsonSchema.Tests.Models.Fr19.User;
using Fr19OffUser = EasyJsonSchema.Tests.Models.Fr19Off.User;

namespace EasyJsonSchema.Tests;

public sealed class GenerateTests
{
    private const string Fr19Schema = """{"properties":[{"name":"Email","type":"String","required":true},{"name":"Status","type":"String","required":true,"enum":["Active","Pending","Disabled"]},{"name":"PreviousStatus","type":"String","enum":["Active","Pending","Disabled"]},{"name":"History","type":"String[]","required":true,"enum":["Active","Pending","Disabled"]}]}""";

    [Fact]
    public void T10_GenerateUser_ValidatesDeserializesAndValidatesData()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<T10User>(out var schemaJson), schemaJson);
        Check.Schema(schemaJson, true);
        Assert.True(Check.Service.DeserializeEasyJsonSchema(schemaJson, out var schema, out var error), error);
        Assert.NotNull(schema);
        Check.Data(schema, """{"Id":7,"Name":"Ada"}""", true);
    }

    [Fact]
    public void T20_UnsupportedTypeAndDeepNesting_FailGeneration()
    {
        T85_UnsupportedMember_Fails();
        T86_NestingBeyondTheLimit_FailsAndReturns();
    }

    [Fact]
    public void T27_MixedMembers_MapToSchemaTypes()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<T27Sample>(out var schemaJson), schemaJson);
        Assert.True(Check.Service.DeserializeEasyJsonSchema(schemaJson, out var schema, out var error), error);
        Assert.NotNull(schema);
        Assert.Equal(4, schema.Properties.Count);

        var color = schema.Properties[0];
        Assert.Equal("Color", color.Name);
        Assert.Equal("String", color.Type);
        Assert.True(color.Required);
        Assert.Equal(["Red", "Blue"], color.Enum);

        var tags = schema.Properties[1];
        Assert.Equal("Tags", tags.Name);
        Assert.Equal("String[]", tags.Type);
        Assert.True(tags.Required);

        var address = schema.Properties[2];
        Assert.Equal("Address", address.Name);
        Assert.Equal("Object", address.Type);
        Assert.True(address.Required);
        Assert.NotNull(address.Properties);
        Assert.Equal("City", address.Properties[0].Name);
        Assert.Equal("String", address.Properties[0].Type);
        Assert.True(address.Properties[0].Required);

        var age = schema.Properties[3];
        Assert.Equal("Age", age.Name);
        Assert.Equal("Int32", age.Type);
        Assert.False(age.Required);

        Check.Schema(schemaJson, true);
        Check.Data(schema, """{"Color":"Red","Tags":["a"],"Address":{"City":"Reykjavik"}}""", true);
    }

    [Fact]
    public void T35_PropertyName_IsUsedUnchanged()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<FirstNameHolder>(out var schemaJson), schemaJson);
        var schema = Check.Load(schemaJson);
        Assert.Equal("FirstName", schema.Properties[0].Name);
        Check.Data(schema, """{"FirstName":"a"}""", true);
        Check.Data(schema, """{"firstname":"a"}""", false, "firstname: additional properties not allowed");
    }

    [Fact]
    public void T37_JsonPropertyName_BecomesTheSchemaName()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<JsonNameIdHolder>(out var schemaJson), schemaJson);
        var schema = Check.Load(schemaJson);
        Assert.Equal("id", schema.Properties[0].Name);
    }

    [Fact]
    public void T38_ReferenceNullability_ControlsRequired()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<NullableOffStringHolder>(out var offJson), offJson);
        var off = Check.Load(offJson);
        Assert.True(off.Properties[0].Required);

        Assert.True(Check.Service.GenerateEasyJsonSchema<NullableOnStringHolder>(out var onJson), onJson);
        var on = Check.Load(onJson);
        Assert.False(on.Properties[0].Required);
        Assert.DoesNotContain("\"required\"", onJson, StringComparison.Ordinal);
    }

    [Fact]
    public void T73_GeneratedSchemas_PassSchemaValidation()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<T10User>(out var t10), t10);
        Check.Schema(t10, true);
        Assert.True(Check.Service.GenerateEasyJsonSchema<T27Sample>(out var t27), t27);
        Check.Schema(t27, true);
    }

    [Fact]
    public void T82_ClassWithoutProperties_Fails()
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema<EmptyClass>(out var error));
        Assert.Equal("$: at least one property required", error);
        Assert.False(Check.Service.GenerateEasyJsonSchema<EmptyNestedHolder>(out error));
        Assert.Equal("Address/properties: properties required for Object", error);
    }

    [Fact]
    public void T83_EnumWithoutMembers_Fails()
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema<EmptyEnumHolder>(out var error));
        Assert.Equal("Status/enum: enum must not be empty", error);
    }

    [Fact]
    public void T84_DuplicateJsonNames_Fail()
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema<DuplicateNameHolder>(out var error));
        Assert.Equal("Id/name: duplicate name", error);
    }

    [Fact]
    public void T85_UnsupportedMember_Fails()
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema<StreamMemberHolder>(out var error));
        Assert.Equal("Data: unsupported member type", error);
    }

    [Fact]
    public void T86_NestingBeyondTheLimit_FailsAndReturns()
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema<AddressRoot>(out var error));
        Assert.Equal("Address.Geo.Point.X: nesting too deep", error);
        Assert.False(Check.Service.GenerateEasyJsonSchema<Node>(out error));
        Assert.Equal("Next.Next.Next.Next: nesting too deep", error);
    }

    [Fact]
    public void T90_EnumListsNullableItemsAndRenamedMembers_RoundTrip()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<ListEnumHolder>(out var listJson), listJson);
        var listSchema = Check.Load(listJson);
        Assert.Equal("String[]", listSchema.Properties[0].Type);
        Assert.Equal(["A", "B"], listSchema.Properties[0].Enum);
        Check.Data(listSchema, """{"Items":["A","B"]}""", true);

        Assert.True(Check.Service.GenerateEasyJsonSchema<NullableListHolder>(out var nullableJson), nullableJson);
        var nullableSchema = Check.Load(nullableJson);
        Assert.Equal("Int32[]", nullableSchema.Properties[0].Type);
        Assert.Equal("String[]", nullableSchema.Properties[1].Type);
        Assert.Equal(["A", "B"], nullableSchema.Properties[1].Enum);
        Check.Data(nullableSchema, """{"Numbers":[1,2],"Enums":["A"]}""", true);

        Assert.True(Check.Service.GenerateEasyJsonSchema<ProgressHolder>(out var progressJson), progressJson);
        var progress = Check.Load(progressJson);
        Assert.Equal("String", progress.Properties[0].Type);
        Assert.Equal(["Todo", "in-progress", "Done"], progress.Properties[0].Enum);
        Check.Data(progress, """{"Progress":"in-progress"}""", true);

        Assert.True(Check.Service.GenerateEasyJsonSchema<ProgressListHolder>(out var historyJson), historyJson);
        var history = Check.Load(historyJson);
        Assert.Equal("String[]", history.Properties[0].Type);
        Assert.Equal(["Todo", "in-progress", "Done"], history.Properties[0].Enum);
        Check.Data(history, """{"History":["Todo","Done"]}""", true);

        Assert.True(Check.Service.Deserialize<ProgressHolder>("""{"Progress":"in-progress"}""", out var value, out var error), error);
        Assert.NotNull(value);
        Assert.Equal(ProgressNamed.InProgress, value.Progress);
    }

    [Fact]
    public void T92_InvalidJsonName_Fails()
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema<InvalidNameHolder>(out var error));
        Assert.Equal("properties[0]/name: invalid name", error);
    }

    [Fact]
    public void T99_Fr19Example_MatchesWithAndWithoutNullableAnnotations()
    {
        Assert.True(Check.Service.GenerateEasyJsonSchema<Fr19User>(out var enabled), enabled);
        Assert.True(Check.Service.GenerateEasyJsonSchema<Fr19OffUser>(out var disabled), disabled);
        Assert.Equal(Fr19Schema, enabled);
        Assert.Equal(Fr19Schema, disabled);
        var schema = Check.Load(enabled);
        Check.Data(schema, """{"Email":"a@b.com","Status":"Active","History":["Pending","Active"]}""", true);
        Check.Data(schema, """{"Email":"a@b.com","Status":"active","History":[]}""", false, "Status: not in enum");
    }

    [Fact]
    public void T101_DuplicateEnumJsonNames_Fail()
    {
        Assert.False(Check.Service.GenerateEasyJsonSchema<DuplicateProgressHolder>(out var error));
        Assert.Equal("Progress/enum: duplicate enum value", error);
        Assert.False(Check.Service.GenerateEasyJsonSchema<DuplicateProgressListHolder>(out error));
        Assert.Equal("History/enum: duplicate enum value", error);
    }
}
