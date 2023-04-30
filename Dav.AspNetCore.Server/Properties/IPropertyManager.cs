using System.Xml.Linq;
using Dav.AspNetCore.Server.Stores;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Properties;

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
    /// Gets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="storeItem">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The property result.</returns>
    ValueTask<PropertyResult> GetPropertyAsync(
        HttpContext context,
        IStoreItem storeItem, 
        XName name, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="storeItem">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    ValueTask<DavStatusCode> SetPropertyAsync(
        HttpContext context,
        IStoreItem storeItem,
        XName name,
        object? value,
        CancellationToken cancellationToken = default);
}