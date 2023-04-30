using System;
using System.Xml.Linq;
using Dav.AspNetCore.Server.Stores;

namespace Dav.AspNetCore.Server.Properties;

public class DefaultPropertyStore : IPropertyStore
{
    /// <summary>
    /// Gets the property metadata for the specified property name.
    /// </summary>
    /// <param name="name">The property name.</param>
    public PropertyMetadata? this[XName name] => throw new NotImplementedException();

    /// <summary>
    /// Gets the property list.
    /// </summary>
    public IReadOnlyCollection<PropertyMetadata> Properties { get; }

    /// <summary>
    /// Accepts a property async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    public Task<DavStatusCode> AcceptPropertyAsync(IStoreItem item, XName name, object? value, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Deletes a property async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    public Task<DavStatusCode> DeletePropertyAsync(IStoreItem item, XName name, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Deletes all properties async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    public Task DeletePropertiesAsync(IStoreItem item, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}