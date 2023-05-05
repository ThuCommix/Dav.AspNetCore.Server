using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

/// <summary>
/// Represents a context for property read operation.
/// </summary>
public class PropertyReadContext
{
    /// <summary>
    /// Initializes a new <see cref="PropertyReadContext"/> class.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="propertyMetadata">The property metadata.</param>
    /// <param name="item">The store item.</param>
    /// <param name="services">The services.</param>
    /// <param name="propertyStore">The property store.</param>
    internal PropertyReadContext(
        XName propertyName,
        PropertyMetadata propertyMetadata,
        IStoreItem item,
        IServiceProvider services,
        IPropertyStore? propertyStore)
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
    /// Gets the result value.
    /// </summary>
    internal object? ResultValue { get; private set; }

    /// <summary>
    /// Gets the result.
    /// </summary>
    internal DavStatusCode Result { get; private set; } = DavStatusCode.NotFound;

    /// <summary>
    /// Indicates that the property value could not be retrieved because it was not found.
    /// </summary>
    public void NotFound() => Fail(DavStatusCode.NotFound);

    /// <summary>
    /// Indicates that its forbidden to read the property value.
    /// </summary>
    public void Forbidden() => Fail(DavStatusCode.Forbidden);

    /// <summary>
    /// Indicates that the user is not allowed to read the property value.
    /// </summary>
    public void Unauthorized() => Fail(DavStatusCode.Unauthorized);

    /// <summary>
    /// Sets the retrieved value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void SetResult(object? value)
    {
        Result = DavStatusCode.Ok;
        ResultValue = value;
    }
    
    private void Fail(DavStatusCode error)
    {
        Result = error;
        ResultValue = null;
    }
}