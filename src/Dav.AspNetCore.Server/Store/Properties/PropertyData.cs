using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

/// <summary>
/// Initializes a new <see cref="PropertyData"/> class.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="CurrentValue">The current value.</param>
public record PropertyData(
    XName Name,
    object? CurrentValue);