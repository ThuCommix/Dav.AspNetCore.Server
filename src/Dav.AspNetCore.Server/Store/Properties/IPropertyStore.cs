using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

public interface IPropertyStore
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
    /// Accepts a property async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    Task<DavStatusCode> AcceptPropertyAsync(
        IStoreItem item,
        XName name,
        object? value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a property async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    Task<DavStatusCode> DeletePropertyAsync(
        IStoreItem item,
        XName name,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes all properties async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    Task DeletePropertiesAsync(
        IStoreItem item,
        CancellationToken cancellationToken = default);
}