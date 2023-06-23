using System.Xml.Linq;

namespace Dav.AspNetCore.Server;

internal record WebDavError(
    ResourcePath Path,
    DavStatusCode StatusCode,
    XElement? ErrorElement);