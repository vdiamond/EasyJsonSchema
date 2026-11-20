namespace EasyJsonSchema.Tests;

public sealed class DataValidationTests
{
    [Fact]
    public void T01_ValidateJsonObject_RequiredInt32_ReturnsTrue()
    {
        Check.Data(Check.Load(Schemas.IdRequired), """{"id":42}""", true);
    }

    [Fact]
    public void T02_ValidateJsonObject_MissingRequired_ReturnsRequired()
    {
        Check.Data(Check.Load(Schemas.IdAndOptionalName), """{"name":"a"}""", false, "id: required");
    }

    [Fact]
    public void T03_ValidateJsonObject_WrongJsonType_ReturnsExpectedType()
    {
        Check.Data(Check.Load(Schemas.NameString), """{"name":5}""", false, "name: expected String, found Number");
    }

    [Fact]
    public void T04_ValidateJsonObject_ValueOutsideEnum_ReturnsNotInEnum()
    {
        Check.Data(Check.Load(Schemas.StatusAb), """{"status":"c"}""", false, "status: not in enum");
    }

    [Fact]
    public void T06_ValidateJsonObject_NestedObject_ChecksRequiredChild()
    {
        var schema = Check.Load(Schemas.Address);
        Check.Data(schema, """{"address":{"zip":"1"}}""", false, "address.city: required");
        Check.Data(schema, """{"address":{"city":"Reykjavik"}}""", true);
    }

    [Fact]
    public void T07_DepthExamples_MatchDataAndSchemaLimits()
    {
        var dataSchema = Check.Load(Schemas.DepthInt);
        Check.Data(dataSchema, """{"a":{"b":{"c":1}}}""", true);
        Check.Data(dataSchema, """{"a":{"b":{"c":{"d":1}}}}""", false, "a.b.c: nesting too deep");
        Check.Schema(Schemas.DepthString, true);
        Check.Schema(Schemas.DepthTooDeep, false, "a.b.c.d: nesting too deep");
    }

    [Fact]
    public void T08_ValidateJsonObject_ExtraProperty_IsRejected()
    {
        Check.Data(Check.Load(Schemas.IdRequired), """{"id":42,"x":1}""", false, "x: additional properties not allowed");
    }

    [Fact]
    public void T09_ValidateJsonObject_Arrays_AcceptValidItemsAndRejectInvalid()
    {
        Check.Data(Check.Load(Schemas.TagsString), """{"tags":["a","b"]}""", true);
        var items = Check.Load(Schemas.ItemsSku);
        Check.Data(items, """{"items":[{"sku":"a"},{"sku":"b"}]}""", true);
        Check.Data(items, """{"items":[{}]}""", false, "items[0]: empty object not allowed");
    }

    [Fact]
    public void T12_EmptyObjectAndEmptyProperties_AreRejected()
    {
        Check.Data(Check.Load(Schemas.IdRequired), "{}", false, "$: empty object not allowed");
        Check.Schema("""{"properties":[]}""", false, "$: at least one property required");
    }

    [Fact]
    public void T13_OmittedRequired_MeansOptional()
    {
        var schema = Check.Load(Schemas.IdAndOptionalName);
        Check.Data(schema, """{"id":1}""", true);
        Assert.True(schema.Properties[0].Required);
        Assert.False(schema.Properties[1].Required);
    }

    [Fact]
    public void T15_RootMustBeObject()
    {
        var schema = Check.Load(Schemas.IdRequired);
        Check.Data(schema, "[1]", false, "$: expected Object, found Array");
        Check.Data(schema, "\"x\"", false, "$: expected Object, found String");
        Check.Data(schema, "1", false, "$: expected Object, found Number");
        Check.Data(schema, "true", false, "$: expected Object, found Boolean");
        Check.Data(schema, "null", false, "$: expected Object, found Null");
    }

    [Fact]
    public void T16_ArrayItems_UseItemRules()
    {
        var tags = Check.Load(Schemas.TagsInt);
        Check.Data(tags, """{"tags":["x"]}""", false, "tags[0]: expected Int32, found String");
        Check.Data(Check.Load(Schemas.TagsString), """{"tags":["a",null]}""", false, "tags[1]: null not allowed");
        Check.Data(tags, """{"tags":[]}""", true);
        Check.Data(tags, """{"tags":"x"}""", false, "tags: expected Int32[], found String");
    }

    [Fact]
    public void T17_DateTime_RejectsNonIsoString()
    {
        Check.Data(
            Check.Load(Schemas.Created),
            """{"created":"yesterday"}""",
            false,
            "created: expected ISO-8601 date-time, e.g. 2026-11-19T14:22:05Z");
    }

    [Fact]
    public void T18_NestedTypeError_UsesDottedPath()
    {
        Check.Data(
            Check.Load(Schemas.Address),
            """{"address":{"city":5}}""",
            false,
            "address.city: expected String, found Number");
    }

    [Fact]
    public void T21_FirstErrorInDataOrder_WinsOverLaterRequired()
    {
        Check.Data(Check.Load(Schemas.IdRequired), """{"x":1}""", false, "x: additional properties not allowed");
    }

    [Fact]
    public void T22_IntegerFormAndRange_AreCheckedInOrder()
    {
        Check.Data(Check.Load(Schemas.ByteN), """{"n":300}""", false, "n: out of range for Byte");
        Check.Data(Check.Load(Schemas.ByteN), """{"n":300.5}""", false, "n: decimal point or exponent not allowed for Byte");
        Check.Data(Check.Load(Schemas.IntN), """{"n":333.2}""", false, "n: decimal point or exponent not allowed for Int32");
        Check.Data(Check.Load(Schemas.IntN), """{"n":42.0}""", false, "n: decimal point or exponent not allowed for Int32");
        Check.Data(Check.Load(Schemas.DoubleN), """{"n":5}""", true);
    }

    [Fact]
    public void T24_NamesAndTypes_AreCaseSensitive()
    {
        Check.Data(Check.Load(Schemas.IdRequired), """{"ID":1}""", false, "ID: additional properties not allowed");
        Check.Schema(
            """{"properties":[{"name":"n","type":"int32"}]}""",
            false,
            "n/type: " + Schemas.UnknownType);
    }

    [Fact]
    public void T25_DuplicateKeys_AreRejected()
    {
        Check.Data(Check.Load(Schemas.IdRequired), """{"id":1,"id":2}""", false, "id: duplicate property");
    }

    [Fact]
    public void T26_MalformedAndEmptyInput_AreRejected()
    {
        Check.Data(Check.Load(Schemas.OptionalA), """{"a":""", false, "$: malformed JSON");
        var schema = Check.Load(Schemas.OptionalA);
        Assert.False(Check.Service.ValidateJsonObject(schema, (string)null!, out var error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateJsonObject(schema, "", out error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateJsonObject(schema, "   ", out error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateJsonObject(schema, ReadOnlySpan<byte>.Empty, out error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateJsonObject(schema, " \t\r\n"u8, out error));
        Assert.Equal("$: no JSON provided", error);
    }

    [Fact]
    public void T31_EnumMatch_IsCaseSensitive()
    {
        var schema = Check.Load(Schemas.StatusActive);
        Check.Data(schema, """{"status":"Active"}""", true);
        Check.Data(schema, """{"status":"active"}""", false, "status: not in enum");
    }

    [Fact]
    public void T36_ExtremeNumbers_FollowJsonSerializer()
    {
        Check.Data(Check.Load(Schemas.IntN), """{"n":1e2}""", false, "n: decimal point or exponent not allowed for Int32");
        AssertFloat<double>(Schemas.DoubleN, "1e400", "Double");
        AssertFloat<decimal>(Schemas.DecimalN, "1e-30", "Decimal");
    }

    [Fact]
    public void T39_EmptyNestedObject_IsRejected()
    {
        Check.Data(Check.Load(Schemas.Address), """{"address":{}}""", false, "address: empty object not allowed");
    }

    [Fact]
    public void T40_NullOptionalObject_IsNotDescended()
    {
        Check.Data(Check.Load(Schemas.Address), """{"address":null}""", true);
    }

    [Fact]
    public void T41_ObjectOfNulls_IsNotEmpty()
    {
        Check.Data(Check.Load(Schemas.OptionalAB), """{"a":null,"b":null}""", true);
    }

    [Fact]
    public void T42_NullStillCountsAsAWrittenKey()
    {
        Check.Data(Check.Load(Schemas.IdRequired), """{"x":null}""", false, "x: additional properties not allowed");
        Check.Data(Check.Load(Schemas.OptionalA), """{"a":null,"a":null}""", false, "a: duplicate property");
    }

    [Fact]
    public void T43_ArraysDoNotAddALevel()
    {
        var schema = Check.Load(Schemas.ArrayDepth);
        Check.Data(schema, """{"a":[{"b":{"c":1}}]}""", true);
        Check.Data(schema, """{"a":[{"b":{"c":{"d":1}}}]}""", false, "a[0].b.c: nesting too deep");
    }

    [Fact]
    public void T44_MissingRequired_UsesSchemaOrderAndPath()
    {
        Check.Data(Check.Load(Schemas.IdAndNameRequired), """{"note":"a"}""", false, "id: required");
        Check.Data(Check.Load(Schemas.Address), """{"address":{"zip":"1"}}""", false, "address.city: required");
        Check.Data(
            Check.Load(Schemas.ItemsSkuQty),
            """{"items":[{"sku":"a"},{"sku":"b"},{"qty":1}]}""",
            false,
            "items[2].sku: required");
    }

    [Fact]
    public void T45_TypeError_WinsOverLaterMalformedJson()
    {
        Check.Data(Check.Load(Schemas.IdRequired), """{"id":"x","a":""", false, "id: expected Int32, found String");
    }

    [Fact]
    public void T46_ReaderRules_RejectCommentsTrailingCommasAndTrailingContent()
    {
        var schema = Check.Load(Schemas.OptionalA);
        Check.Data(schema, """{"a":1} xyz""", false, "$: malformed JSON");
        Check.Data(schema, """{"a":1,}""", false, "$: malformed JSON");
        Check.Data(schema, """{/*c*/"a":1}""", false, "$: malformed JSON");
        Check.Data(schema, "{\"a\":1} \n", true);
    }

    [Fact]
    public void T47_LeadingBom_IsSkippedOnBothOverloads()
    {
        var schema = Check.Load(Schemas.OptionalA);
        Assert.True(Check.Service.ValidateJsonObject(schema, "\uFEFF{\"a\":1}", out var error), error);
        Assert.Equal(string.Empty, error);

        var body = Encoding.UTF8.GetBytes("{\"a\":1}");
        var bytes = new byte[body.Length + 3];
        bytes[0] = 0xEF;
        bytes[1] = 0xBB;
        bytes[2] = 0xBF;
        body.CopyTo(bytes, 3);
        Assert.True(Check.Service.ValidateJsonObject(schema, bytes, out error), error);
        Assert.Equal(string.Empty, error);

        Assert.False(Check.Service.ValidateJsonObject(schema, "\uFEFF", out error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateJsonObject(schema, "\uFEFF \t", out error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateJsonObject(schema, new byte[] { 0xEF, 0xBB, 0xBF }, out error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateJsonObject(schema, new byte[] { 0xEF, 0xBB, 0xBF, 0x20, 0x09 }, out error));
        Assert.Equal("$: no JSON provided", error);

        Check.Data(schema, "\uFEFF\uFEFF{\"a\":1}", false, "$: malformed JSON");
        var doubled = new byte[body.Length + 6];
        doubled[0] = 0xEF;
        doubled[1] = 0xBB;
        doubled[2] = 0xBF;
        doubled[3] = 0xEF;
        doubled[4] = 0xBB;
        doubled[5] = 0xBF;
        body.CopyTo(doubled, 6);
        Assert.False(Check.Service.ValidateJsonObject(schema, doubled, out error));
        Assert.Equal("$: malformed JSON", error);
    }

    [Fact]
    public void T48_StringsAndEmptyEnumValues_FollowExactMatch()
    {
        var text = Check.Load(Schemas.NameString);
        Check.Data(text, """{"name":""}""", true);
        Check.Data(text, """{"name":"   "}""", true);
        Check.Data(Check.Load(Schemas.StatusAb), """{"status":""}""", false, "status: not in enum");
        Check.Data(Check.Load(Schemas.EmptyStringEnum), """{"status":""}""", true);
    }

    [Fact]
    public void T51_NullSchema_IsReportedFirst()
    {
        Assert.False(Check.Service.ValidateJsonObject(null!, """{"a":1}""", out var error));
        Assert.Equal("$: schema is null", error);
        Assert.False(Check.Service.ValidateJsonObject(null!, (string)null!, out error));
        Assert.Equal("$: schema is null", error);
        Assert.False(Check.Service.ValidateJsonObject(null!, """{"a":1}"""u8, out error));
        Assert.Equal("$: schema is null", error);
        Assert.False(Check.Service.ValidateJsonObject(null!, ReadOnlySpan<byte>.Empty, out error));
        Assert.Equal("$: schema is null", error);
    }

    [Fact]
    public void T74_RequiredOnLevel3Leaf_UsesTheSameRules()
    {
        var schema = Check.Load(Schemas.Level3Leaf);
        Check.Data(schema, """{"a":{"b":{"x":"y"}}}""", false, "a.b.c: required");
        Check.Data(schema, """{"a":{"b":{"c":null}}}""", false, "a.b.c: null not allowed");
        Check.Data(schema, """{"a":{"b":{"c":"z"}}}""", true);
    }

    [Fact]
    public void T89_StringArrayEnum_ChecksEachItem()
    {
        var schema = Check.Load(Schemas.TagsStringEnum);
        Check.Data(schema, """{"tags":["a","b"]}""", true);
        Check.Data(schema, """{"tags":["a","c"]}""", false, "tags[1]: not in enum");
        Check.Data(schema, """{"tags":[]}""", true);
        Check.Data(schema, """{"tags":[""]}""", false, "tags[0]: not in enum");
        Check.Data(schema, """{"tags":["a",null]}""", false, "tags[1]: null not allowed");
    }

    [Fact]
    public void T95_EnumMatch_UsesUnescapedBytes()
    {
        var status = Check.Load(Schemas.StatusAb);
        Check.Data(status, "{\"status\":\"\\u0061\"}", true);
        Check.Data(status, "{\"status\":\"\\u0063\"}", false, "status: not in enum");
        Check.Data(Check.Load(Schemas.TagsStringEnum), "{\"tags\":[\"\\u0061\",\"\\u0062\"]}", true);
    }

    [Fact]
    public void T100_DepthAndNull_DifferBetweenDataAndSchema()
    {
        var schema = Check.Load(Schemas.DepthString);
        Check.Data(schema, """{"a":{"b":{"c":"x"}}}""", true);
        Check.Data(schema, """{"a":{"b":{"c":{"d":1}}}}""", false, "a.b.c: nesting too deep");
        Check.Schema(Schemas.DepthString, true);
        Check.Schema(Schemas.DepthTooDeep, false, "a.b.c.d: nesting too deep");
        Check.Data(Check.Load(Schemas.OptionalAge), """{"age":null}""", true);
        Check.Schema(
            """{"properties":[{"name":"age","type":"Int32","required":null}]}""",
            false,
            "age/required: null not allowed");
    }

    #region private

    private static void AssertFloat<T>(string schemaJson, string literal, string typeName)
    {
        var accepted = true;
        try
        {
            JsonSerializer.Deserialize<T>(literal);
        }
        catch (JsonException)
        {
            accepted = false;
        }

        var json = "{\"n\":" + literal + "}";
        if (accepted)
        {
            Check.Data(Check.Load(schemaJson), json, true);
            return;
        }

        Check.Data(Check.Load(schemaJson), json, false, "n: out of range for " + typeName);
    }

    #endregion
}
