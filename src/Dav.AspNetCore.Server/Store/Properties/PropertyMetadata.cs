namespace Dav.AspNetCore.Server.Store.Properties;

/// <summary>
/// Initializes a new <see cref="PropertyMetadata"/> class.
/// </summary>
/// <param name="DefaultValue">The property default value.</param>
/// <param name="Expensive">A value indicating whether that the retrieval of the property is considered expensive.</param>
/// <param name="Protected">A value indicating whether that the property is protected.</param>
/// <param name="Computed">A value indicating whether that the property is computed.</param>
public record PropertyMetadata(
    object? DefaultValue = null,
    bool Expensive = false,
    bool Protected = false,
    bool Computed = false)
{
    /// <summary>
    /// Gets the default property metadata.
    /// </summary>
    public static PropertyMetadata Default { get; } = new();
}