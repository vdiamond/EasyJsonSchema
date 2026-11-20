namespace EasyJsonSchema;

/// <summary>
/// A parsed EasyJsonSchema document. The cached instance is the compiled schema.
/// </summary>
public sealed class EasyJsonSchema
{
    /// <summary>
    /// Gets or sets the optional schema title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the optional schema description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets free text stored in the <c>$meta</c> field. The library does not interpret it.
    /// </summary>
    [JsonPropertyName("$meta")]
    public string? Meta { get; set; }

    /// <summary>
    /// Gets or sets the property definitions. The schema format requires at least one.
    /// </summary>
    public List<PropertyDef> Properties { get; set; } = new();
}
