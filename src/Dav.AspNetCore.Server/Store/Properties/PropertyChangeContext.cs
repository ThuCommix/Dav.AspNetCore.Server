using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

/// <summary>
/// Represents a context for property change operation.
/// </summary>
public class PropertyChangeContext
{
    /// <summary>
    /// Initializes a new <see cref="PropertyChangeContext"/> class.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="propertyMetadata">The property metadata.</param>
    /// <param name="item">The store item.</param>
    /// <param name="services">The services.</param>
    /// <param name="propertyStore">The property store.</param>
    /// <param name="value">The value.</param>
    internal PropertyChangeContext(
        XName propertyName,
        PropertyMetadata propertyMetadata,
        IStoreItem item,
        IServiceProvider services,
        IPropertyStore? propertyStore,
        object? value)
    {
        ArgumentNullException.ThrowIfNull(propertyName, nameof(propertyName));
        ArgumentNullException.ThrowIfNull(propertyMetadata, nameof(propertyMetadata));
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        
        PropertyName = propertyName;
        PropertyMetadata = propertyMetadata;
        Item = item;
        Services = services;
        PropertyStore = propertyStore;
        Value = value;
    }

    /// <summary>
    /// Gets the property name.
    /// </summary>
    public XName PropertyName { get; }
    
    /// <summary>
    /// Gets the property metadata.
    /// </summary>
    public PropertyMetadata PropertyMetadata { get; }
    
    /// <summary>
    /// Gets the store item.
    /// </summary>
    public IStoreItem Item { get; }
    
    /// <summary>
    /// Gets the services.
    /// </summary>
    public IServiceProvider Services { get; }
    
    /// <summary>
    /// Gets the property store.
    /// </summary>
    public IPropertyStore? PropertyStore { get; }
    
    /// <summary>
    /// Gets the value.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the result.
    /// </summary>
    internal DavStatusCode Result { get; private set; } = DavStatusCode.Ok;

    /// <summary>
    /// Indicates that its forbidden to change the property value.
    /// </summary>
    public void Forbidden() => Result = DavStatusCode.Forbidden;

    /// <summary>
    /// Indicates that the property value does not match the semantics of the property.
    /// </summary>
    public void Conflict() => Result = DavStatusCode.Conflict;

    /// <summary>
    /// Indicates that the operation could not be completed because the server ran out of storage.
    /// </summary>
    public void InsufficientStorage() => Result = DavStatusCode.InsufficientStorage;

    /// <summary>
    /// Indicates that the user is not allowed to change the property value.
    /// </summary>
    public void Unauthorized() => Result = DavStatusCode.Unauthorized;
}