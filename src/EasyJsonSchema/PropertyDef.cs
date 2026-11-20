namespace EasyJsonSchema;

/// <summary>
/// One property definition in an EasyJsonSchema document.
/// </summary>
public sealed class PropertyDef
{
    /// <summary>
    /// Gets or sets the property name. Unique among its siblings, matched exactly.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the schema type name, for example <c>Int32</c>, <c>String[]</c>, <c>Object</c>, or <c>Object[]</c>.
    /// </summary>
    public string Type { get; set; } = "";

    /// <summary>
    /// Gets or sets whether the property must be present. Omitted values stay <see langword="false"/>.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the optional title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets free text stored in the <c>$meta</c> field. The library does not interpret it.
    /// </summary>
    [JsonPropertyName("$meta")]
    public string? Meta { get; set; }

    /// <summary>
    /// Gets or sets the allowed strings. Used only for <c>String</c> and <c>String[]</c>.
    /// </summary>
    public List<string>? Enum { get; set; }

    /// <summary>
    /// Gets or sets nested definitions. Used only for <c>Object</c> and <c>Object[]</c>.
    /// </summary>
    public List<PropertyDef>? Properties { get; set; }
}
