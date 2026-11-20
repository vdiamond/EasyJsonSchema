namespace EasyJsonSchema;

internal static class JsonKinds
{
    internal const string Object = "Object";

    internal const string Array = "Array";

    internal const string String = "String";

    internal const string Number = "Number";

    internal const string Boolean = "Boolean";

    internal const string Null = "Null";

    internal static string Of(JsonTokenType token)
    {
        return token switch
        {
            JsonTokenType.StartObject or JsonTokenType.EndObject => Object,
            JsonTokenType.StartArray or JsonTokenType.EndArray => Array,
            JsonTokenType.String => String,
            JsonTokenType.Number => Number,
            JsonTokenType.True or JsonTokenType.False => Boolean,
            JsonTokenType.Null => Null,
            _ => throw new InvalidOperationException("Unexpected JSON token."),
        };
    }
}
