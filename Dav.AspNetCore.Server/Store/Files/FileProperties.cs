namespace Dav.AspNetCore.Server.Store.Files;

public record FileProperties(
    Uri Uri,
    string Name,
    DateTime Created,
    DateTime LastModified,
    long Length);