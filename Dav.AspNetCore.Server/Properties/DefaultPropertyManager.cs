using System.Xml.Linq;
using Dav.AspNetCore.Server.Stores;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Properties;

public class DefaultPropertyManager<T> : IPropertyManager where T : IStoreItem
{
    private readonly Dictionary<XName, Property<T>> properties;

    /// <summary>
    /// Initializes a new <see cref="DefaultPropertyManager{T}"/> class.
    /// </summary>
    /// <param name="properties">The properties.</param>
    public DefaultPropertyManager(IEnumerable<Property<T>> properties)
    {
        ArgumentNullException.ThrowIfNull(properties, nameof(properties));

        this.properties = properties.ToDictionary(x => x.Name);
        Properties = this.properties
            .Select(x => new PropertyMetadata(x.Key, x.Value.IsExpensive, x.Value.IsProtected, x.Value.IsCalculated)).ToList();
    }

    /// <summary>
    /// Gets the property metadata for the specified property name.
    /// </summary>
    /// <param name="name">The property name.</param>
    public PropertyMetadata? this[XName name]
    {
        get
        {
            if (properties.TryGetValue(name, out var property))
            {
                return new PropertyMetadata(
                    name, 
                    property.IsExpensive, 
                    property.IsProtected, 
                    property.IsCalculated);
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the property list.
    /// </summary>
    public IReadOnlyCollection<PropertyMetadata> Properties { get; }

    /// <summary>
    /// Gets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="storeItem">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The property result.</returns>
    ValueTask<PropertyResult> IPropertyManager.GetPropertyAsync(
        HttpContext context,
        IStoreItem storeItem,
        XName name,
        CancellationToken cancellationToken)
        => GetPropertyAsync(context, (T)storeItem, name, cancellationToken);

    /// <summary>
    /// Sets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="storeItem">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    ValueTask<DavStatusCode> IPropertyManager.SetPropertyAsync(
        HttpContext context,
        IStoreItem storeItem,
        XName name,
        object? value,
        CancellationToken cancellationToken)
        => SetPropertyAsync(context, (T)storeItem, name, value, cancellationToken);

    /// <summary>
    /// Gets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="resourceItem">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The property result.</returns>
    protected virtual ValueTask<PropertyResult> GetPropertyAsync(
        HttpContext context,
        T resourceItem,
        XName name,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resourceItem, nameof(resourceItem));
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        if (properties.TryGetValue(name, out var property))
        {
            return property.GetValueAsync?.Invoke(context, resourceItem, cancellationToken) 
                   ?? ValueTask.FromResult(PropertyResult.Success(null));
        }

        return ValueTask.FromResult(PropertyResult.Fail(DavStatusCode.NotFound));
    }
    
    /// <summary>
    /// Sets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="resourceItem">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    protected virtual ValueTask<DavStatusCode> SetPropertyAsync(
        HttpContext context,
        T resourceItem, 
        XName name,
        object? value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resourceItem, nameof(resourceItem));
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        if (properties.TryGetValue(name, out var property))
        {
            return property.SetValueAsync?.Invoke(context, resourceItem, value, cancellationToken) 
                   ?? ValueTask.FromResult(DavStatusCode.Conflict);
        }
        
        return ValueTask.FromResult(DavStatusCode.NotFound);
    }
}