namespace EasyJsonSchema.Tests;

public sealed class SchemaValidationTests
{
    [Fact]
    public void T05_EnumOnInt32_IsRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"n","type":"Int32","enum":["1"]}]}""",
            false,
            "n/enum: enum only allowed on String or String[]");
    }

    [Fact]
    public void T19_EachSchemaRule_HasExactPathAndText()
    {
        T55_UnknownType_IsRejected();
        T56_EnumOnNonString_IsRejected();
        T57_EmptyEnum_IsRejected();
        T58_DuplicateEnumValue_IsRejected();
        T59_PropertiesOnScalar_AreRejected();
        T60_ObjectWithoutProperties_IsRejected();
        T61_DuplicateNames_AreRejectedAndCaseDiffers();
        T62_EmptyRootProperties_AreRejected();
        T65_UnknownField_IsRejected();
        T66_WrongFieldCase_UsesTheIndex();
        T67_MissingNameOrType_IsRequired();
        T68_EmptyObjects_AreRejected();
        T76_NestedError_WinsOverTheParent();
        T77_NameLookAhead_FindsALaterName();
        T78_DuplicateNameKey_UsesTheFirstName();
        T79_MissingOrNonStringName_UsesTheIndex();
        T80_MalformedSchema_WinsOverEarlierTypeErrors();
        T81_NullSchemaValues_AreRejected();
        T87_InvalidNames_AreRejectedAndOrdinaryNamesPass();
        T88_EnumOnStringArray_IsAllowed();
        T93_RootFieldPaths_UseDollar();
        T102_SchemaFieldTypes_AreChecked();
    }

    [Fact]
    public void T33_ObjectWithoutProperties_IsRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"address","type":"Object"}]}""",
            false,
            "address/properties: properties required for Object");
        Check.Schema(
            """{"properties":[{"name":"address","type":"Object","properties":[]}]}""",
            false,
            "address/properties: properties required for Object");
    }

    [Fact]
    public void T53_FlatSchema_IsValid()
    {
        Check.Schema(Schemas.FlatValid, true);
    }

    [Fact]
    public void T54_SchemaNestedToLevel3_IsValid()
    {
        Check.Schema(Schemas.DepthString, true);
    }

    [Fact]
    public void T55_UnknownType_IsRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"age","type":"Int33"}]}""",
            false,
            "age/type: " + Schemas.UnknownType);
        Check.Schema(
            """{"properties":[{"name":"age","type":"int32"}]}""",
            false,
            "age/type: " + Schemas.UnknownType);
    }

    [Fact]
    public void T56_EnumOnNonString_IsRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"age","type":"Int32","enum":["1"]}]}""",
            false,
            "age/enum: enum only allowed on String or String[]");
    }

    [Fact]
    public void T57_EmptyEnum_IsRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"status","type":"String","enum":[]}]}""",
            false,
            "status/enum: enum must not be empty");
    }

    [Fact]
    public void T58_DuplicateEnumValue_IsRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"status","type":"String","enum":["a","b","a"]}]}""",
            false,
            "status/enum: duplicate enum value");
    }

    [Fact]
    public void T59_PropertiesOnScalar_AreRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"age","type":"Int32","properties":[{"name":"x","type":"String"}]}]}""",
            false,
            "age/properties: properties not allowed for Int32");
    }

    [Fact]
    public void T60_ObjectWithoutProperties_IsRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"address","type":"Object"}]}""",
            false,
            "address/properties: properties required for Object");
        Check.Schema(
            """{"properties":[{"name":"items","type":"Object[]","properties":[]}]}""",
            false,
            "items/properties: properties required for Object[]");
    }

    [Fact]
    public void T61_DuplicateNames_AreRejectedAndCaseDiffers()
    {
        Check.Schema(
            """{"properties":[{"name":"id","type":"Int32"},{"name":"id","type":"String"}]}""",
            false,
            "id/name: duplicate name");
        Check.Schema(
            """{"properties":[{"name":"id","type":"Int32"},{"name":"ID","type":"String"}]}""",
            true);
    }

    [Fact]
    public void T62_EmptyRootProperties_AreRejected()
    {
        Check.Schema("""{"properties":[]}""", false, "$: at least one property required");
    }

    [Fact]
    public void T63_DefinitionAtLevel4_IsTooDeep()
    {
        Check.Schema(Schemas.DepthTooDeep, false, "a.b.c.d: nesting too deep");
    }

    [Fact]
    public void T64_ObjectAtLevel3WithoutProperties_RequiresProperties()
    {
        Check.Schema(
            """{"properties":[{"name":"a","type":"Object","properties":[{"name":"b","type":"Object","properties":[{"name":"c","type":"Object"}]}]}]}""",
            false,
            "a.b.c/properties: properties required for Object");
    }

    [Fact]
    public void T65_UnknownField_IsRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"age","type":"Int32","minLength":1}]}""",
            false,
            "age/minLength: additional properties not allowed");
    }

    [Fact]
    public void T66_WrongFieldCase_UsesTheIndex()
    {
        Check.Schema(
            """{"properties":[{"Name":"id","type":"Int32"}]}""",
            false,
            "properties[0]/Name: additional properties not allowed");
    }

    [Fact]
    public void T67_MissingNameOrType_IsRequired()
    {
        Check.Schema(
            """{"properties":[{"name":"id"}]}""",
            false,
            "id/type: required");
        Check.Schema(
            """{"properties":[{"type":"Int32"}]}""",
            false,
            "properties[0]/name: required");
    }

    [Fact]
    public void T68_EmptyObjects_AreRejected()
    {
        Check.Schema("{}", false, "$: empty object not allowed");
        Check.Schema("""{"properties":[{}]}""", false, "properties[0]: empty object not allowed");
    }

    [Fact]
    public void T69_MalformedSchema_ReturnsMalformedJson()
    {
        Check.Schema("""{"properties":""", false, "$: malformed JSON");
    }

    [Fact]
    public void T70_DefinitionRuleOrder_UsesTheLowestNumber()
    {
        Check.Schema(
            """{"properties":[{"name":"n","type":"Int32","enum":[]}]}""",
            false,
            "n/enum: enum only allowed on String or String[]");
        Check.Schema(
            """{"properties":[{"name":"n","type":"Int33","enum":["a"]}]}""",
            false,
            "n/type: " + Schemas.UnknownType);
    }

    [Fact]
    public void T71_MissingName_WinsOverUnknownType()
    {
        Check.Schema(
            """{"properties":[{"type":"Int33"}]}""",
            false,
            "properties[0]/name: required");
    }

    [Fact]
    public void T75_EmptySchemaInput_IsRejected()
    {
        Assert.False(Check.Service.ValidateEasyJsonSchema(null!, out var error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateEasyJsonSchema("", out error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateEasyJsonSchema("   ", out error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateEasyJsonSchema("\uFEFF", out error));
        Assert.Equal("$: no JSON provided", error);
    }

    [Fact]
    public void T76_NestedError_WinsOverTheParent()
    {
        Check.Schema(
            """{"properties":[{"name":"age","type":"Int32","properties":[{"name":"x","type":"Int33"}]}]}""",
            false,
            "age.x/type: " + Schemas.UnknownType);
    }

    [Fact]
    public void T77_NameLookAhead_FindsALaterName()
    {
        Check.Schema(
            """{"properties":[{"type":"Int33","name":"age"}]}""",
            false,
            "age/type: " + Schemas.UnknownType);
        Check.Schema(
            """{"properties":[{"type":"Object","properties":[{"name":"x","type":"Int33"}],"name":"a"}]}""",
            false,
            "a.x/type: " + Schemas.UnknownType);
    }

    [Fact]
    public void T78_DuplicateNameKey_UsesTheFirstName()
    {
        Check.Schema(
            """{"properties":[{"name":"a","name":"b","type":"Int32"}]}""",
            false,
            "a/name: duplicate property");
    }

    [Fact]
    public void T79_MissingOrNonStringName_UsesTheIndex()
    {
        Check.Schema(
            """{"properties":[{"type":"Object","properties":[{"name":"x","type":"Int33"}]}]}""",
            false,
            "properties[0].x/type: " + Schemas.UnknownType);
        Check.Schema(
            """{"properties":[{"name":5,"type":"Int32"}]}""",
            false,
            "properties[0]/name: expected String, found Number");
    }

    [Fact]
    public void T80_MalformedSchema_WinsOverEarlierTypeErrors()
    {
        Check.Schema("""{"properties":[{"name":"n","type":5},""", false, "$: malformed JSON");
        Check.Schema("""{"properties":[{"name":"n","type":5}]} xyz""", false, "$: malformed JSON");
    }

    [Fact]
    public void T81_NullSchemaValues_AreRejected()
    {
        Check.Schema(
            """{"properties":[{"name":"age","type":"Int32","required":null}]}""",
            false,
            "age/required: null not allowed");
        Check.Schema(
            """{"properties":[{"name":"s","type":"String","enum":null}]}""",
            false,
            "s/enum: null not allowed");
        Check.Schema(
            """{"properties":[{"name":"address","type":"Object","properties":null}]}""",
            false,
            "address/properties: null not allowed");
        Check.Schema(
            """{"title":null,"properties":[{"name":"a","type":"Int32"}]}""",
            false,
            "$/title: null not allowed");
        Check.Schema(
            """{"properties":[{"name":"s","type":"String","enum":["a",null]}]}""",
            false,
            "s/enum[1]: null not allowed");
        Check.Schema("""{"properties":[null]}""", false, "properties[0]: null not allowed");
    }

    [Fact]
    public void T87_InvalidNames_AreRejectedAndOrdinaryNamesPass()
    {
        foreach (var name in new[] { "", "a.b", "a/b", "a[0]", "a]" })
        {
            var json = "{\"properties\":[{\"name\":" + JsonSerializer.Serialize(name) + ",\"type\":\"Int32\"}]}";
            Check.Schema(json, false, "properties[0]/name: invalid name");
        }

        Check.Schema("""{"properties":[{"name":"user-id","type":"String"}]}""", true);
        Check.Schema("""{"properties":[{"name":"first name","type":"String"}]}""", true);
    }

    [Fact]
    public void T88_EnumOnStringArray_IsAllowed()
    {
        Check.Schema("""{"properties":[{"name":"tags","type":"String[]","enum":["a","b"]}]}""", true);
        Check.Schema(
            """{"properties":[{"name":"n","type":"Int32[]","enum":["1"]}]}""",
            false,
            "n/enum: enum only allowed on String or String[]");
    }

    [Fact]
    public void T93_RootFieldPaths_UseDollar()
    {
        Check.Schema("""{"title":"x"}""", false, "$/properties: required");
        Check.Schema(
            """{"properties":[{"name":"a","type":"Int32"}],"foo":1}""",
            false,
            "$/foo: additional properties not allowed");
        Check.Schema("""{"properties":null}""", false, "$/properties: null not allowed");
        Check.Schema("{}", false, "$: empty object not allowed");
        Check.Schema("""{"properties":[]}""", false, "$: at least one property required");
    }

    [Fact]
    public void T94_InvalidAncestorName_UsesTheIndex()
    {
        Check.Schema(
            """{"properties":[{"name":"a.b","type":"Object","properties":[{"name":"x","type":"Int33"}]}]}""",
            false,
            "properties[0].x/type: " + Schemas.UnknownType);
    }

    [Fact]
    public void T102_SchemaFieldTypes_AreChecked()
    {
        Check.Schema(
            """{"title":5,"properties":[{"name":"a","type":"Int32"}]}""",
            false,
            "$/title: expected String, found Number");
        Check.Schema(
            """{"properties":[{"name":"a","type":5}]}""",
            false,
            "a/type: expected String, found Number");
        Check.Schema(
            """{"properties":[{"name":"age","type":"Int32","required":"yes"}]}""",
            false,
            "age/required: expected Boolean, found String");
        Check.Schema(
            """{"properties":[{"name":"s","type":"String","enum":"a"}]}""",
            false,
            "s/enum: expected String[], found String");
        Check.Schema(
            """{"properties":[{"name":"s","type":"String","enum":[1]}]}""",
            false,
            "s/enum[0]: expected String, found Number");
        Check.Schema("""{"properties":"x"}""", false, "$/properties: expected Object[], found String");
        Check.Schema("""{"properties":[5]}""", false, "properties[0]: expected Object, found Number");
    }
}
