using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

public interface IPropertyManager
{
    /// <summary>
    /// Gets the property metadata.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property metadata or null when the property was not found.</returns>
    PropertyMetadata? GetPropertyMetadata(IStoreItem item, XName name);
    
    /// <summary>
    /// Gets the property async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The property result.</returns>
    ValueTask<PropertyResult> GetPropertyAsync(
        IStoreItem item,
        XName name, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the property async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    ValueTask<DavStatusCode> SetPropertyAsync(
        IStoreItem item,
        XName name,
        object? value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the property names async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of property names.</returns>
    ValueTask<IReadOnlyCollection<XName>> GetPropertyNamesAsync(IStoreItem item, CancellationToken cancellationToken = default);
}