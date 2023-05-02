using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

/// <summary>
/// Initializes a new <see cref="PropertyMetadata"/> class.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="IsExpensive">A value indicating whether the property is expensive to retrieve.</param>
/// <param name="IsProtected">A value indicating whether the property is protected.</param>
/// <param name="IsComputed">A value indicating whether the property is calculated.</param>
public record PropertyMetadata(
    XName Name,
    bool IsExpensive,
    bool IsProtected,
    bool IsComputed);