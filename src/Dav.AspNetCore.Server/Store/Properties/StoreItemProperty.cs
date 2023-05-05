using System.Xml.Linq;

namespace Dav.AspNetCore.Server.Store.Properties;

public record StoreItemProperty(
    XName Name,
    PropertyMetadata Metadata);