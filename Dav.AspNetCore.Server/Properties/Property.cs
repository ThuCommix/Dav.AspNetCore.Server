using System.Xml.Linq;
using Dav.AspNetCore.Server.Stores;
using Microsoft.AspNetCore.Http;

namespace Dav.AspNetCore.Server.Properties;

/// <summary>
/// Initializes a new <see cref="Property{T}"/> class.
/// </summary>
/// <typeparam name="T">The store item type.</typeparam>
public abstract class Property<T> where T : IStoreItem
{
    /// <summary>
    /// Initializes a new <see cref="Property{T}"/> class.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <typeparam name="T">The store item type.</typeparam>
    protected Property(XName name)
    {
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        Name = name;
    }
    
    /// <summary>
    /// Gets the property name.
    /// </summary>
    public XName Name { get; }
    
    /// <summary>
    /// A value indicating whether the property value is expensive to calculate.
    /// </summary>
    /// <remarks>If <see cref="IsExpensive"/> is set to true, it will be skipped during all prop requests.</remarks>
    public bool IsExpensive { get; set; }
    
    /// <summary>
    /// A value indicating whether the property value is protected.
    /// </summary>
    public bool IsProtected { get; set; }
    
    /// <summary>
    /// A value indicating whether the property value is calculated.
    /// </summary>
    public bool IsCalculated { get; set; }
    
    /// <summary>
    /// Gets the value setter.
    /// </summary>
    public Func<HttpContext, T, object?, CancellationToken, ValueTask<DavStatusCode>>? SetValueAsync { get; set; }

    /// <summary>
    /// Gets the value getter.
    /// </summary>
    public Func<HttpContext, T, CancellationToken, ValueTask<PropertyResult>>? GetValueAsync { get; set; }
}