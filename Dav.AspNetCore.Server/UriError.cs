using System.Xml.Linq;

namespace Dav.AspNetCore.Server;

internal record UriError(
    Uri Uri,
    DavStatusCode StatusCode,
    XElement? ErrorElement);