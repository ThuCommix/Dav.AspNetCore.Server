using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

public class PropertyManager : IPropertyManager
{
    private readonly IServiceProvider services;
    private readonly IPropertyStore? propertyStore;
    private readonly Dictionary<IStoreItem, Dictionary<XName, PropertyResult>> propertyCache = new();

    /// <summary>
    /// Initializes a new <see cref="PropertyManager"/> class.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="propertyStore">The property store.</param>
    public PropertyManager(
        IServiceProvider services,
        IPropertyStore? propertyStore = null)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        
        this.services = services;
        this.propertyStore = propertyStore;
    }

    /// <summary>
    /// Gets the property metadata.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <returns>The property metadata or null when the property was not found.</returns>
    public PropertyMetadata? GetPropertyMetadata(IStoreItem item, XName name)
    {
        if (Property.Registrations.TryGetValue(item.GetType(), out var propertyMap))
        {
            if (propertyMap.TryGetValue(name, out var property))
                return property.Metadata;
        }

        return null;
    }

    /// <summary>
    /// Gets the property async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The property result.</returns>
    public async ValueTask<PropertyResult> GetPropertyAsync(
        IStoreItem item, 
        XName name, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(name, nameof(name));

        if (propertyCache.TryGetValue(item, out var cache) &&
            cache.TryGetValue(name, out var cachedValue))
            return cachedValue;

        Property? property = null;
        if (Property.Registrations.TryGetValue(item.GetType(), out var propertyMap))
        {
            propertyMap.TryGetValue(name, out property);
        }

        if (property != null && property.ReadCallback != null)
        {
            var context = new PropertyReadContext(
                name,
                property.Metadata,
                item,
                services,
                propertyStore);

            await property.ReadCallback(context, cancellationToken);
            return CreateCachedResult(item, name, context.Result, context.ResultValue);
        }

        if (property != null && propertyStore == null)
            return CreateCachedResult(item, name, DavStatusCode.Ok, property.Metadata.DefaultValue);

        if (propertyStore == null)
            return CreateCachedResult(item, name, DavStatusCode.NotFound, null);

        var propertyDataList = await propertyStore.GetPropertiesAsync(item, cancellationToken);
        var propertyData = propertyDataList.FirstOrDefault(x => x.Name == name);
        return propertyData == null
            ? CreateCachedResult(item, name, DavStatusCode.NotFound, null)
            : CreateCachedResult(item, name, DavStatusCode.Ok, propertyData.CurrentValue);
    }

    /// <summary>
    /// Sets the property async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The status code.</returns>
    public async ValueTask<DavStatusCode> SetPropertyAsync(
        IStoreItem item, 
        XName name, 
        object? value, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        
        Property? property = null;
        if (Property.Registrations.TryGetValue(item.GetType(), out var propertyMap))
        {
            propertyMap.TryGetValue(name, out property);
        }

        if (property != null && (property.Metadata.Protected || property.Metadata.Computed))
            return DavStatusCode.Forbidden;

        if (property != null && property.ChangeCallback != null)
        {
            var context = new PropertyChangeContext(
                name,
                property.Metadata,
                item,
                services,
                propertyStore,
                value);

            await property.ChangeCallback(context, cancellationToken);
            if (context.Result == DavStatusCode.Ok)
                CreateCachedResult(item, name, DavStatusCode.Ok, value);
            
            return context.Result;
        }

        if (property != null && propertyStore == null)
            return DavStatusCode.Forbidden;

        if (propertyStore == null)
            return DavStatusCode.NotFound;
        
        var propertyDataList = await propertyStore.GetPropertiesAsync(item, cancellationToken);
        var propertyData = propertyDataList.FirstOrDefault(x => x.Name == name);
        if (propertyData == null)
            return DavStatusCode.NotFound;

        await propertyStore.SetPropertyAsync(
            item, 
            propertyData.Name, 
            PropertyMetadata.Default, 
            value, 
            cancellationToken);

        CreateCachedResult(item, name, DavStatusCode.Ok, value);
        
        return DavStatusCode.Ok;
    }

    /// <summary>
    /// Gets the property names async.
    /// </summary>
    /// <param name="item">The store item.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of property names.</returns>
    public async ValueTask<IReadOnlyCollection<XName>> GetPropertyNamesAsync(IStoreItem item, CancellationToken cancellationToken = default)
    {
        var results = new HashSet<XName>();
        if (Property.Registrations.TryGetValue(item.GetType(), out var propertyMap))
        {
            foreach (var name in propertyMap.Keys)
            {
                results.Add(name);
            }
        }

        if (propertyStore != null)
        {
            var propertyDataList = await propertyStore.GetPropertiesAsync(item, cancellationToken);
            foreach (var propertyData in propertyDataList)
            {
                results.Add(propertyData.Name);
            }
        }

        return results;
    }

    private PropertyResult CreateCachedResult(
        IStoreItem item,
        XName propertyName,
        DavStatusCode statusCode, 
        object? value)
    {
        var result = new PropertyResult(statusCode, value);
        if (!propertyCache.ContainsKey(item))
            propertyCache[item] = new Dictionary<XName, PropertyResult>();
        
        propertyCache[item][propertyName] = result;
        
        return result;
    }
}