using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

public interface IPropertyStore
{
    /// <summary>
    /// Commits the property store changes async.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all properties of the specified item async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    ValueTask DeletePropertiesAsync(IStoreItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies all properties of the specified item to the destination async.
    /// </summary>
    /// <param name="source">The source store item.</param>
    /// <param name="destination">The destination store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    ValueTask CopyPropertiesAsync(IStoreItem source, IStoreItem destination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a property of the specified item async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="propertyMetadata">The property metadata.</param>
    /// <param name="isRegistered">A value indicating whether the property is registered.</param>
    /// <param name="propertyValue">The property value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns></returns>
    ValueTask<bool> SetPropertyAsync(
        IStoreItem item,
        XName propertyName,
        PropertyMetadata propertyMetadata,
        bool isRegistered,
        object? propertyValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the properties of the specified item async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of stored properties.</returns>
    ValueTask<IReadOnlyCollection<PropertyData>> GetPropertiesAsync(
        IStoreItem item,
        CancellationToken cancellationToken = default);
}