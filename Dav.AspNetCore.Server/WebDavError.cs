using System.Xml.Linq;

namespace Dav.AspNetCore.Server;

internal record WebDavError(
    Uri Uri,
    DavStatusCode StatusCode,
    XElement? ErrorElement);