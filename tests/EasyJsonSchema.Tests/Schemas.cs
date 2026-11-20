namespace EasyJsonSchema.Tests;

internal static class Schemas
{
    internal const string IdRequired = """{"properties":[{"name":"id","type":"Int32","required":true}]}""";

    internal const string IdAndOptionalName = """{"properties":[{"name":"id","type":"Int32","required":true},{"name":"name","type":"String"}]}""";

    internal const string NameString = """{"properties":[{"name":"name","type":"String"}]}""";

    internal const string StatusAb = """{"properties":[{"name":"status","type":"String","enum":["a","b"]}]}""";

    internal const string StatusActive = """{"properties":[{"name":"status","type":"String","enum":["Active"]}]}""";

    internal const string Address = """{"properties":[{"name":"address","type":"Object","properties":[{"name":"city","type":"String","required":true},{"name":"zip","type":"String"}]}]}""";

    internal const string DepthInt = """{"properties":[{"name":"a","type":"Object","properties":[{"name":"b","type":"Object","properties":[{"name":"c","type":"Int32"}]}]}]}""";

    internal const string DepthString = """{"properties":[{"name":"a","type":"Object","properties":[{"name":"b","type":"Object","properties":[{"name":"c","type":"String"}]}]}]}""";

    internal const string DepthTooDeep = """{"properties":[{"name":"a","type":"Object","properties":[{"name":"b","type":"Object","properties":[{"name":"c","type":"Object","properties":[{"name":"d","type":"String"}]}]}]}]}""";

    internal const string TagsString = """{"properties":[{"name":"tags","type":"String[]"}]}""";

    internal const string TagsStringEnum = """{"properties":[{"name":"tags","type":"String[]","enum":["a","b"]}]}""";

    internal const string TagsInt = """{"properties":[{"name":"tags","type":"Int32[]"}]}""";

    internal const string ItemsSku = """{"properties":[{"name":"items","type":"Object[]","properties":[{"name":"sku","type":"String","required":true}]}]}""";

    internal const string ItemsSkuQty = """{"properties":[{"name":"items","type":"Object[]","properties":[{"name":"sku","type":"String","required":true},{"name":"qty","type":"Int32"}]}]}""";

    internal const string OptionalA = """{"properties":[{"name":"a","type":"Int32"}]}""";

    internal const string OptionalAB = """{"properties":[{"name":"a","type":"Int32"},{"name":"b","type":"Int32"}]}""";

    internal const string Created = """{"properties":[{"name":"created","type":"DateTime","required":true}]}""";

    internal const string ByteN = """{"properties":[{"name":"n","type":"Byte"}]}""";

    internal const string IntN = """{"properties":[{"name":"n","type":"Int32"}]}""";

    internal const string DoubleN = """{"properties":[{"name":"n","type":"Double"}]}""";

    internal const string DecimalN = """{"properties":[{"name":"n","type":"Decimal"}]}""";

    internal const string IdAndAge = """{"properties":[{"name":"id","type":"Int32","required":true},{"name":"age","type":"Int32"}]}""";

    internal const string OptionalAge = """{"properties":[{"name":"age","type":"Int32"}]}""";

    internal const string ArrayDepth = """{"properties":[{"name":"a","type":"Object[]","properties":[{"name":"b","type":"Object","properties":[{"name":"c","type":"Int32"}]}]}]}""";

    internal const string IdAndNameRequired = """{"properties":[{"name":"id","type":"Int32","required":true},{"name":"name","type":"String","required":true},{"name":"note","type":"String"}]}""";

    internal const string Level3Leaf = """{"properties":[{"name":"a","type":"Object","properties":[{"name":"b","type":"Object","properties":[{"name":"c","type":"String","required":true},{"name":"x","type":"String"}]}]}]}""";

    internal const string FlatValid = """{"properties":[{"name":"id","type":"Int32","required":true},{"name":"name","type":"String"}]}""";

    internal const string EmptyStringEnum = """{"properties":[{"name":"status","type":"String","enum":[""]}]}""";

    internal const string UnknownType = "unknown type (types are case-sensitive, e.g. Int32)";
}
