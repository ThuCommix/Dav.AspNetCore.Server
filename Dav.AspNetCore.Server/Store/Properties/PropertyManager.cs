using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Store.Properties;

public class PropertyManager : IPropertyManager
{
    private readonly Dictionary<XName, AttachedProperty> properties;
    private readonly List<PropertyMetadata> metadataList;
    private readonly Dictionary<XName, PropertyResult> propertyCache = new();
    private readonly IStoreItem item;

    /// <summary>
    /// Initializes a new <see cref="PropertyManager"/> class.
    /// </summary>
    /// <param name="item">The store item.</param>
    public PropertyManager(IStoreItem item)
        : this(item, Array.Empty<AttachedProperty>())
    {
    }
    
    /// <summary>
    /// Initializes a new <see cref="PropertyManager"/> class.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="properties">The properties.</param>
    public PropertyManager(
        IStoreItem item,
        IEnumerable<AttachedProperty> properties)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(properties, nameof(properties));

        this.item = item;
        this.properties = properties.ToDictionary(x => x.Metadata.Name);
        metadataList = this.properties
            .Select(x => x.Value.Metadata).ToList();
    }

    /// <summary>
    /// Gets the property metadata for the specified property name.
    /// </summary>
    /// <param name="name">The property name.</param>
    [IndexerName("Indexer")]
    public PropertyMetadata? this[XName name] 
        => properties.TryGetValue(name, out var property) ? property.Metadata : null;

    /// <summary>
    /// Gets the property list.
    /// </summary>
    public IReadOnlyCollection<PropertyMetadata> Properties => metadataList.AsReadOnly();
    
    /// <summary>
    /// Attaches a property.
    /// </summary>
    /// <param name="property">The property.</param>
    public void AttachProperty(AttachedProperty property)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));
        
        if (properties.ContainsKey(property.Metadata.Name))
            throw new InvalidOperationException("The property is already attached.");
        
        properties.Add(property.Metadata.Name, property);
        metadataList.Add(property.Metadata);
    }

    /// <summary>
    /// Detaches a property based on the given property metadata.
    /// </summary>
    /// <param name="metadata">The property metadata.</param>
    /// <returns>True when the property was detached, otherwise false.</returns>
    public bool DetachProperty(PropertyMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata, nameof(metadata));

        if (!properties.ContainsKey(metadata.Name))
            return false;

        if (!metadataList.Remove(metadata))
            return false;

        properties.Remove(metadata.Name);

        return true;
    }

    /// <summary>
    /// Gets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The property result.</returns>
    ValueTask<PropertyResult> IPropertyManager.GetPropertyAsync(
        HttpContext context,
        XName name,
        CancellationToken cancellationToken)
        => GetPropertyAsync(context, name, cancellationToken);

    /// <summary>
    /// Sets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    ValueTask<DavStatusCode> IPropertyManager.SetPropertyAsync(
        HttpContext context,
        XName name,
        object? value,
        CancellationToken cancellationToken)
        => SetPropertyAsync(context, name, value, cancellationToken);

    /// <summary>
    /// Gets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The property result.</returns>
    protected virtual async ValueTask<PropertyResult> GetPropertyAsync(
        HttpContext context,
        XName name,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        if (properties.TryGetValue(name, out var property))
        {
            if (property.GetValueAsync == null)
                return PropertyResult.Success(null);
            
            if (!propertyCache.ContainsKey(name)) 
                propertyCache[name] = await property.GetValueAsync(context, item, cancellationToken);

            return propertyCache[name];
        }

        return PropertyResult.Fail(DavStatusCode.NotFound);
    }
    
    /// <summary>
    /// Sets the property async.
    /// </summary>
    /// <param name="context">The http context.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    protected virtual async ValueTask<DavStatusCode> SetPropertyAsync(
        HttpContext context,
        XName name,
        object? value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        if (properties.TryGetValue(name, out var property))
        {
            if (property.SetValueAsync == null)
                return DavStatusCode.Forbidden;
            
            var result = await property.SetValueAsync.Invoke(context, item, value, cancellationToken);
            if (result == DavStatusCode.Ok)
                propertyCache[name] = new PropertyResult(DavStatusCode.Ok, value);

            return result;
        }
        
        return DavStatusCode.NotFound;
    }
}