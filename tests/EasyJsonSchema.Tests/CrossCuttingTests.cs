using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Xunit.Abstractions;

namespace EasyJsonSchema.Tests;

public sealed class CrossCuttingTests
{
    public CrossCuttingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task T29_OneCachedSchema_IsSafeAcrossThreads()
    {
        var schemas = new Dictionary<string, SchemaDocument>(StringComparer.Ordinal)
        {
            ["id"] = Check.Load(Schemas.IdRequired),
            ["idName"] = Check.Load(Schemas.IdAndOptionalName),
            ["name"] = Check.Load(Schemas.NameString),
            ["status"] = Check.Load(Schemas.StatusAb),
            ["address"] = Check.Load(Schemas.Address),
            ["depth"] = Check.Load(Schemas.DepthInt),
            ["tags"] = Check.Load(Schemas.TagsString),
            ["items"] = Check.Load(Schemas.ItemsSku),
        };

        var baseline = RunCore(schemas);
        var mismatches = 0;
        var tasks = new Task[16];
        for (var thread = 0; thread < tasks.Length; thread++)
        {
            tasks[thread] = Task.Run(() =>
            {
                for (var iteration = 0; iteration < 1000; iteration++)
                {
                    if (RunCore(schemas) != baseline)
                    {
                        Interlocked.Increment(ref mismatches);
                        return;
                    }
                }
            });
        }

        await Task.WhenAll(tasks);
        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void T30_Utf8Overload_MatchesTheStringOverload()
    {
        var schemas = LoadCoreSchemas();
        RunCore(schemas);
    }

    [Fact]
    public void T34_FailureCases_ReturnFalseWithoutThrowing()
    {
        var data = new DataValidationTests();
        data.T02_ValidateJsonObject_MissingRequired_ReturnsRequired();
        data.T03_ValidateJsonObject_WrongJsonType_ReturnsExpectedType();
        data.T04_ValidateJsonObject_ValueOutsideEnum_ReturnsNotInEnum();
        data.T06_ValidateJsonObject_NestedObject_ChecksRequiredChild();
        data.T07_DepthExamples_MatchDataAndSchemaLimits();
        data.T08_ValidateJsonObject_ExtraProperty_IsRejected();
        data.T09_ValidateJsonObject_Arrays_AcceptValidItemsAndRejectInvalid();
        data.T12_EmptyObjectAndEmptyProperties_AreRejected();
        data.T15_RootMustBeObject();
        data.T16_ArrayItems_UseItemRules();
        data.T17_DateTime_RejectsNonIsoString();
        data.T18_NestedTypeError_UsesDottedPath();
        data.T21_FirstErrorInDataOrder_WinsOverLaterRequired();
        data.T22_IntegerFormAndRange_AreCheckedInOrder();
        data.T24_NamesAndTypes_AreCaseSensitive();
        data.T25_DuplicateKeys_AreRejected();
        data.T26_MalformedAndEmptyInput_AreRejected();
        data.T36_ExtremeNumbers_FollowJsonSerializer();
        data.T39_EmptyNestedObject_IsRejected();
        data.T42_NullStillCountsAsAWrittenKey();
        data.T43_ArraysDoNotAddALevel();
        data.T44_MissingRequired_UsesSchemaOrderAndPath();
        data.T45_TypeError_WinsOverLaterMalformedJson();
        data.T46_ReaderRules_RejectCommentsTrailingCommasAndTrailingContent();
        data.T48_StringsAndEmptyEnumValues_FollowExactMatch();
        data.T51_NullSchema_IsReportedFirst();
        data.T74_RequiredOnLevel3Leaf_UsesTheSameRules();
        data.T89_StringArrayEnum_ChecksEachItem();
        data.T95_EnumMatch_UsesUnescapedBytes();
        data.T100_DepthAndNull_DifferBetweenDataAndSchema();

        var schema = new SchemaValidationTests();
        schema.T05_EnumOnInt32_IsRejected();
        schema.T33_ObjectWithoutProperties_IsRejected();
        schema.T55_UnknownType_IsRejected();
        schema.T56_EnumOnNonString_IsRejected();
        schema.T57_EmptyEnum_IsRejected();
        schema.T58_DuplicateEnumValue_IsRejected();
        schema.T59_PropertiesOnScalar_AreRejected();
        schema.T60_ObjectWithoutProperties_IsRejected();
        schema.T61_DuplicateNames_AreRejectedAndCaseDiffers();
        schema.T62_EmptyRootProperties_AreRejected();
        schema.T63_DefinitionAtLevel4_IsTooDeep();
        schema.T64_ObjectAtLevel3WithoutProperties_RequiresProperties();
        schema.T65_UnknownField_IsRejected();
        schema.T66_WrongFieldCase_UsesTheIndex();
        schema.T67_MissingNameOrType_IsRequired();
        schema.T68_EmptyObjects_AreRejected();
        schema.T69_MalformedSchema_ReturnsMalformedJson();
        schema.T70_DefinitionRuleOrder_UsesTheLowestNumber();
        schema.T71_MissingName_WinsOverUnknownType();
        schema.T75_EmptySchemaInput_IsRejected();
        schema.T76_NestedError_WinsOverTheParent();
        schema.T77_NameLookAhead_FindsALaterName();
        schema.T78_DuplicateNameKey_UsesTheFirstName();
        schema.T79_MissingOrNonStringName_UsesTheIndex();
        schema.T80_MalformedSchema_WinsOverEarlierTypeErrors();
        schema.T81_NullSchemaValues_AreRejected();
        schema.T87_InvalidNames_AreRejectedAndOrdinaryNamesPass();
        schema.T88_EnumOnStringArray_IsAllowed();
        schema.T93_RootFieldPaths_UseDollar();
        schema.T94_InvalidAncestorName_UsesTheIndex();
        schema.T102_SchemaFieldTypes_AreChecked();

        var generated = new GenerateTests();
        generated.T20_UnsupportedTypeAndDeepNesting_FailGeneration();
        generated.T82_ClassWithoutProperties_Fails();
        generated.T83_EnumWithoutMembers_Fails();
        generated.T84_DuplicateJsonNames_Fail();
        generated.T85_UnsupportedMember_Fails();
        generated.T86_NestingBeyondTheLimit_FailsAndReturns();
        generated.T92_InvalidJsonName_Fails();
        generated.T101_DuplicateEnumJsonNames_Fail();

        var deserialized = new DeserializeTests();
        deserialized.T52_Deserialize_ReportsInputAndSerializerFaults();
        deserialized.T91_DeserializeEasyJsonSchema_DoesNotValidate();
        deserialized.T97_EnumProperties_ReadNamesAndRejectNumbers();

        var v22 = new SpecV22Tests();
        v22.T103_ObjectSupport_AcceptsUserClassesAndRejectsTheRest();
        v22.T104_Arrays_MapExceptByteArray();
        v22.T105_TypesOutsideTheTable_AreUnsupported();
        v22.T106_JsonIgnoreAndStaticMembers_AreSelectedByTheReadRules();
        v22.T107_NullType_ReturnsTypeIsNull();
        v22.T108_JsonNull_CannotDeserialize();
        v22.T109_DuplicateJsonNames_MapInvalidOperationToCannotDeserialize();
        v22.T110_Whitespace_IsJsonWhitespaceOnly();
    }

    [Fact]
    public void T50_ProblemTexts_IncludeRootNestedAndArrayPaths()
    {
        var schema = Check.Load(Schemas.OptionalA);
        Assert.False(Check.Service.ValidateJsonObject(schema, "   ", out var error));
        Assert.Equal("$: no JSON provided", error);
        Assert.False(Check.Service.ValidateJsonObject(null!, """{"a":1}""", out error));
        Assert.Equal("$: schema is null", error);
        Check.Data(schema, """{"a":""", false, "$: malformed JSON");

        Assert.False(Check.Service.Deserialize<Models.Stored.User>("""{"nickname":1}""", out var user, out error));
        Assert.Null(user);
        Assert.Equal("$: cannot deserialize to User", error);

        Check.Data(Check.Load(Schemas.IdAndNameRequired), """{"note":"a"}""", false, "id: required");
        Check.Data(Check.Load(Schemas.Address), """{"address":{"zip":"1"}}""", false, "address.city: required");
        Check.Data(Check.Load(Schemas.ItemsSkuQty), """{"items":[{"sku":"a"},{"sku":"b"},{"qty":1}]}""", false, "items[2].sku: required");

        Check.Data(Check.Load(Schemas.IdRequired), """{"id":null}""", false, "id: null not allowed");
        Check.Data(Check.Load(Schemas.Address), """{"address":{"city":null}}""", false, "address.city: null not allowed");
        Check.Data(Check.Load(Schemas.TagsString), """{"tags":["a",null]}""", false, "tags[1]: null not allowed");

        Check.Data(Check.Load(Schemas.IdRequired), "{}", false, "$: empty object not allowed");
        Check.Data(Check.Load(Schemas.Address), """{"address":{}}""", false, "address: empty object not allowed");
        Check.Data(Check.Load(Schemas.ItemsSku), """{"items":[{}]}""", false, "items[0]: empty object not allowed");

        Check.Data(Check.Load(Schemas.IdRequired), """{"x":1}""", false, "x: additional properties not allowed");
        Check.Data(Check.Load(Schemas.Address), """{"address":{"city":"a","extra":1}}""", false, "address.extra: additional properties not allowed");
        Check.Data(Check.Load(Schemas.ItemsSku), """{"items":[{"sku":"a","extra":1}]}""", false, "items[0].extra: additional properties not allowed");

        Check.Data(Check.Load(Schemas.IdRequired), """{"id":1,"id":2}""", false, "id: duplicate property");
        var nestedId = Check.Load("""{"properties":[{"name":"address","type":"Object","properties":[{"name":"id","type":"Int32"}]}]}""");
        Check.Data(nestedId, """{"address":{"id":1,"id":2}}""", false, "address.id: duplicate property");
        var itemId = Check.Load("""{"properties":[{"name":"items","type":"Object[]","properties":[{"name":"id","type":"Int32"}]}]}""");
        Check.Data(itemId, """{"items":[{"id":1,"id":2}]}""", false, "items[0].id: duplicate property");

        Check.Data(Check.Load(Schemas.NameString), """{"name":5}""", false, "name: expected String, found Number");
        Check.Data(Check.Load(Schemas.Address), """{"address":{"city":5}}""", false, "address.city: expected String, found Number");
        Check.Data(Check.Load(Schemas.TagsInt), """{"tags":["x"]}""", false, "tags[0]: expected Int32, found String");
        Check.Data(Check.Load(Schemas.IdRequired), "[1]", false, "$: expected Object, found Array");
        Check.Data(Check.Load(Schemas.Created), """{"created":1}""", false, "created: expected DateTime, found Number");
        Check.Data(Check.Load(Schemas.Created), """{"created":"nope"}""", false, "created: expected ISO-8601 date-time, e.g. 2026-11-19T14:22:05Z");
        Check.Data(Check.Load(Schemas.ByteN), """{"n":300}""", false, "n: out of range for Byte");
        Check.Data(Check.Load(Schemas.IntN), """{"n":1e2}""", false, "n: decimal point or exponent not allowed for Int32");
        Check.Data(Check.Load(Schemas.StatusAb), """{"status":"c"}""", false, "status: not in enum");
        Check.Data(Check.Load(Schemas.TagsStringEnum), """{"tags":["a","c"]}""", false, "tags[1]: not in enum");
        Check.Data(Check.Load(Schemas.DepthInt), """{"a":{"b":{"c":{"d":1}}}}""", false, "a.b.c: nesting too deep");
        Check.Data(Check.Load(Schemas.ArrayDepth), """{"a":[{"b":{"c":{"d":1}}}]}""", false, "a[0].b.c: nesting too deep");

        Assert.False(Check.Service.GenerateEasyJsonSchema<Models.StreamMemberHolder>(out var generated));
        Assert.Equal("Data: unsupported member type", generated);
    }

    [Fact]
    public void T72_ValidatedSchema_LoadsAndValidatesLevel2Data()
    {
        Check.Schema(Schemas.DepthString, true);
        Assert.True(Check.Service.DeserializeEasyJsonSchema(Schemas.DepthString, out var schema, out var error), error);
        Assert.NotNull(schema);
        Check.Data(schema, """{"a":{"b":{"c":"x"}}}""", true);
    }

    [Fact]
    public void Nfr5_HotPathBenchmark_IsReported()
    {
        const string schemaJson = """{"properties":[{"name":"id","type":"Int32","required":true},{"name":"name","type":"String","required":true},{"name":"status","type":"String","required":true,"enum":["active","pending"]},{"name":"tags","type":"String[]"},{"name":"address","type":"Object","required":true,"properties":[{"name":"city","type":"String","required":true},{"name":"zip","type":"String"}]}]}""";
        var schema = Check.Load(schemaJson);
        var json = """{"id":42,"name":"Ada","status":"active","tags":["a","b"],"address":{"city":"Reykjavik","zip":"101"}}"""u8.ToArray();

        for (var index = 0; index < 2_000; index++)
        {
            Assert.True(Check.Service.ValidateJsonObject(schema, json, out _));
        }

        const int iterations = 20_000;
        var stopwatch = new Stopwatch();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Start();
        for (var index = 0; index < iterations; index++)
        {
            if (!Check.Service.ValidateJsonObject(schema, json, out var error))
            {
                throw new InvalidOperationException(error);
            }
        }

        stopwatch.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var nanoseconds = stopwatch.Elapsed.TotalMilliseconds * 1_000_000d / iterations;
        var bytesPerCall = allocated / (double)iterations;
        var flatReport = string.Format(
            CultureInfo.InvariantCulture,
            "NFR-5 flat schema: {0} iterations, {1:F1} ns/op, {2:F1} allocated bytes/op, schema cached, UTF-8 span.",
            iterations,
            nanoseconds,
            bytesPerCall);
        _output.WriteLine(flatReport);
        Console.WriteLine(flatReport);

        const string nestedSchemaJson = """{"properties":[{"name":"items","type":"Object[]","required":true,"properties":[{"name":"sku","type":"String","required":true},{"name":"tags","type":"String[]"},{"name":"detail","type":"Object","required":true,"properties":[{"name":"city","type":"String","required":true}]}]}]}""";
        var nestedSchema = Check.Load(nestedSchemaJson);
        var nestedJson = """{"items":[{"sku":"a","tags":["x","y"],"detail":{"city":"Reykjavik"}},{"sku":"b","tags":["z"],"detail":{"city":"Oslo"}}]}"""u8.ToArray();
        for (var index = 0; index < 2_000; index++)
        {
            Assert.True(Check.Service.ValidateJsonObject(nestedSchema, nestedJson, out _));
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        stopwatch.Restart();
        for (var index = 0; index < iterations; index++)
        {
            if (!Check.Service.ValidateJsonObject(nestedSchema, nestedJson, out var nestedError))
            {
                throw new InvalidOperationException(nestedError);
            }
        }

        stopwatch.Stop();
        allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        var nestedNanoseconds = stopwatch.Elapsed.TotalMilliseconds * 1_000_000d / iterations;
        var nestedBytes = allocated / (double)iterations;
        var nestedReport = string.Format(
            CultureInfo.InvariantCulture,
            "NFR-5 nested object arrays: {0} iterations, {1:F1} ns/op, {2:F1} allocated bytes/op, schema cached, UTF-8 span.",
            iterations,
            nestedNanoseconds,
            nestedBytes);
        _output.WriteLine(nestedReport);
        Console.WriteLine(nestedReport);
    }

    [Fact]
    public void ScalarRangesAndMetadata_AreAccepted()
    {
        Check.Data(Check.Load(Schemas.ByteN), """{"n":0}""", true);
        Check.Data(Check.Load(Schemas.ByteN), """{"n":255}""", true);
        Check.Data(Check.Load(Schemas.ByteN), """{"n":-0}""", true);
        Check.Data(Check.Load(Schemas.ByteN), """{"n":-1}""", false, "n: out of range for Byte");
        Check.Data(Check.Load(Schemas.ByteN), """{"n":256}""", false, "n: out of range for Byte");

        var int16 = Check.Load("""{"properties":[{"name":"n","type":"Int16"}]}""");
        Check.Data(int16, """{"n":-32768}""", true);
        Check.Data(int16, """{"n":32767}""", true);
        Check.Data(int16, """{"n":32768}""", false, "n: out of range for Int16");

        var int64 = Check.Load("""{"properties":[{"name":"n","type":"Int64"}]}""");
        Check.Data(int64, """{"n":-9223372036854775808}""", true);
        Check.Data(int64, """{"n":9223372036854775807}""", true);
        Check.Data(int64, """{"n":9223372036854775808}""", false, "n: out of range for Int64");

        Check.Data(Check.Load("""{"properties":[{"name":"n","type":"Boolean"}]}"""), """{"n":false}""", true);
        Check.Schema(
            """{"title":"User","description":"A user","$meta":"indexed","properties":[{"name":"id","type":"Int32","title":"Id","description":"Identifier","$meta":"pk","required":false}]}""",
            true);

        Assert.True(Check.Service.GenerateEasyJsonSchema<Models.ScalarBag>(out var generated), generated);
        var schema = Check.Load(generated);
        Assert.Equal(
            ["Boolean", "Byte", "Int16", "Int32", "Int64", "Single", "Double", "Decimal", "DateTime", "String", "Int32[]", "Boolean[]"],
            schema.Properties.Select(property => property.Type).ToArray());
    }

    [Fact]
    public void Section11TestMethods_ExistForActiveTestIds()
    {
        var names = typeof(Check).Assembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Select(method => method.Name)
            .ToList();

        for (var id = 1; id <= 110; id++)
        {
            if (id is 11 or 28)
            {
                continue;
            }

            var prefix = "T" + id.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + "_";
            Assert.Contains(names, name => name.StartsWith(prefix, StringComparison.Ordinal));
        }
    }

    #region private

    private readonly ITestOutputHelper _output;

    private static Dictionary<string, SchemaDocument> LoadCoreSchemas()
    {
        return new Dictionary<string, SchemaDocument>(StringComparer.Ordinal)
        {
            ["id"] = Check.Load(Schemas.IdRequired),
            ["idName"] = Check.Load(Schemas.IdAndOptionalName),
            ["name"] = Check.Load(Schemas.NameString),
            ["status"] = Check.Load(Schemas.StatusAb),
            ["address"] = Check.Load(Schemas.Address),
            ["depth"] = Check.Load(Schemas.DepthInt),
            ["tags"] = Check.Load(Schemas.TagsString),
            ["items"] = Check.Load(Schemas.ItemsSku),
        };
    }

    private static string RunCore(Dictionary<string, SchemaDocument> schemas)
    {
        var builder = new StringBuilder();
        Record(builder, schemas["id"], """{"id":42}""");
        Record(builder, schemas["idName"], """{"name":"a"}""");
        Record(builder, schemas["name"], """{"name":5}""");
        Record(builder, schemas["status"], """{"status":"c"}""");
        Record(builder, schemas["address"], """{"address":{"zip":"1"}}""");
        Record(builder, schemas["address"], """{"address":{"city":"Reykjavik"}}""");
        Record(builder, schemas["depth"], """{"a":{"b":{"c":1}}}""");
        Record(builder, schemas["depth"], """{"a":{"b":{"c":{"d":1}}}}""");
        RecordSchema(builder, Schemas.DepthString);
        RecordSchema(builder, Schemas.DepthTooDeep);
        Record(builder, schemas["id"], """{"id":42,"x":1}""");
        Record(builder, schemas["tags"], """{"tags":["a","b"]}""");
        Record(builder, schemas["items"], """{"items":[{"sku":"a"}]}""");
        Record(builder, schemas["items"], """{"items":[{}]}""");
        return builder.ToString();
    }

    private static void Record(StringBuilder builder, SchemaDocument schema, string json)
    {
        var ok = Check.Service.ValidateJsonObject(schema, json, out var error);
        var bytes = Encoding.UTF8.GetBytes(json);
        var okBytes = Check.Service.ValidateJsonObject(schema, bytes, out var errorBytes);
        Assert.Equal(ok, okBytes);
        Assert.Equal(error, errorBytes);
        builder.Append(ok);
        builder.Append('|');
        builder.Append(error);
        builder.Append('|');
        builder.Append(okBytes);
        builder.Append('|');
        builder.Append(errorBytes);
        builder.Append('\n');
    }

    private static void RecordSchema(StringBuilder builder, string schemaJson)
    {
        var ok = Check.Service.ValidateEasyJsonSchema(schemaJson, out var error);
        builder.Append(ok);
        builder.Append('|');
        builder.Append(error);
        builder.Append('\n');
    }

    #endregion
}
