using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Store.Properties;

public interface IPropertyManager
{
    /// <summary>
    /// Gets the property metadata for the specified property name.
    /// </summary>
    /// <param name="name">The property name.</param>
    PropertyMetadata? this[XName name] { get; } 
    
    /// <summary>
    /// Gets the property list.
    /// </summary>
    IReadOnlyCollection<PropertyMetadata> Properties { get; }

    /// <summary>
    /// Attaches a property.
    /// </summary>
    /// <param name="property">The property.</param>
    void AttachProperty(AttachedProperty property);

    /// <summary>
    /// Detaches a property based on the given property metadata.
    /// </summary>
    /// <param name="metadata">The property metadata.</param>
    /// <returns>True when the property was detached, otherwise false.</returns>
    bool DetachProperty(PropertyMetadata metadata);

    /// <summary>
    /// Gets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The property result.</returns>
    ValueTask<PropertyResult> GetPropertyAsync(
        HttpContext context,
        XName name, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    ValueTask<DavStatusCode> SetPropertyAsync(
        HttpContext context,
        XName name,
        object? value,
        CancellationToken cancellationToken = default);
}