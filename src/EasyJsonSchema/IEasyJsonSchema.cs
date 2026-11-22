namespace EasyJsonSchema;

/// <summary>
/// Validates JSON objects, checks schema documents, deserializes stored JSON, generates schemas, and writes objects.
/// </summary>
public interface IEasyJsonSchema
{
    /// <summary>
    /// Validates <paramref name="jsonObject"/> against <paramref name="schema"/>.
    /// </summary>
    /// <param name="schema">The cached schema. A null schema is a developer fault.</param>
    /// <param name="jsonObject">The JSON object text.</param>
    /// <param name="error">Empty when the object is valid; otherwise the first error.</param>
    /// <returns><see langword="true"/> when the object matches the schema.</returns>
    bool ValidateJsonObject(EasyJsonSchema schema, string jsonObject, out string error);

    /// <summary>
    /// Validates UTF-8 JSON bytes against <paramref name="schema"/> without copying the caller's buffer.
    /// </summary>
    /// <param name="schema">The cached schema. A null schema is a developer fault.</param>
    /// <param name="utf8JsonObject">The UTF-8 JSON object.</param>
    /// <param name="error">Empty when the object is valid; otherwise the first error.</param>
    /// <returns><see langword="true"/> when the object matches the schema.</returns>
    bool ValidateJsonObject(EasyJsonSchema schema, ReadOnlySpan<byte> utf8JsonObject, out string error);

    /// <summary>
    /// Reads stored JSON into <typeparamref name="T"/>. It does not take a schema and does not validate.
    /// A JSON literal <c>null</c> returns <see langword="false"/> with <c>$: cannot deserialize to &lt;TypeName&gt;</c>.
    /// </summary>
    /// <typeparam name="T">The target type. <c>&lt;TypeName&gt;</c> is <c>typeof(T).Name</c>.</typeparam>
    /// <param name="json">The stored JSON text.</param>
    /// <param name="value">The object when reading succeeds; otherwise <c>default</c>.</param>
    /// <param name="error">Empty when reading succeeds; otherwise the error.</param>
    /// <returns><see langword="true"/> when the JSON becomes an instance of <typeparamref name="T"/>.</returns>
    bool Deserialize<T>(string json, out T? value, out string error);

    /// <summary>
    /// Validates a schema document.
    /// </summary>
    /// <param name="schemaJson">The schema document text.</param>
    /// <param name="error">Empty when the document is valid; otherwise the first error.</param>
    /// <returns><see langword="true"/> when the document matches the format.</returns>
    bool ValidateEasyJsonSchema(string schemaJson, out string error);

    /// <summary>
    /// Reads a schema document into <see cref="EasyJsonSchema"/>. It does not validate the schema.
    /// A JSON literal <c>null</c> returns <see langword="false"/> with <c>$: cannot deserialize to EasyJsonSchema</c>, and <paramref name="schema"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="schemaJson">The schema document text.</param>
    /// <param name="schema">The schema when reading succeeds; otherwise <see langword="null"/>.</param>
    /// <param name="error">Empty when reading succeeds; otherwise the error.</param>
    /// <returns><see langword="true"/> when the document becomes a schema object.</returns>
    bool DeserializeEasyJsonSchema(string schemaJson, out EasyJsonSchema? schema, out string error);

    /// <summary>
    /// Builds schema JSON from the public properties of <paramref name="type"/>.
    /// A null <paramref name="type"/> returns <see langword="false"/> with <c>$: type is null</c>.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <param name="errorOrSchema">The schema JSON when generation succeeds; otherwise the error.</param>
    /// <param name="writeIndented"><see langword="false"/> writes compact JSON. <see langword="true"/> indents with two spaces and a line feed.</param>
    /// <returns><see langword="true"/> when generation succeeds.</returns>
    bool GenerateEasyJsonSchema(Type type, out string errorOrSchema, bool writeIndented = false);

    /// <summary>
    /// Builds schema JSON from the public properties of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="errorOrSchema">The schema JSON when generation succeeds; otherwise the error.</param>
    /// <param name="writeIndented"><see langword="false"/> writes compact JSON. <see langword="true"/> indents with two spaces and a line feed.</param>
    /// <returns><see langword="true"/> when generation succeeds.</returns>
    bool GenerateEasyJsonSchema<T>(out string errorOrSchema, bool writeIndented = false);

    /// <summary>
    /// Writes <paramref name="value"/> as JSON, with C# enums as names, and checks it against the schema generated for <typeparamref name="T"/>.
    /// A null value returns <see langword="false"/> with <c>$: value is null</c>. On every failure <paramref name="json"/> is empty.
    /// </summary>
    /// <typeparam name="T">The type to write. <c>&lt;TypeName&gt;</c> is <c>typeof(T).Name</c>.</typeparam>
    /// <param name="value">The object to write.</param>
    /// <param name="json">The JSON when writing succeeds; otherwise empty.</param>
    /// <param name="error">Empty when writing succeeds; otherwise the error.</param>
    /// <param name="writeIndented"><see langword="false"/> writes compact JSON. <see langword="true"/> indents with two spaces and a line feed.</param>
    /// <returns><see langword="true"/> when the JSON is written and passes the generated schema.</returns>
    bool Serialize<T>(T value, out string json, out string error, bool writeIndented = false);
}
