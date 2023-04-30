namespace Dav.AspNetCore.Server.Stores.Files;

public record FileProperties(
    Uri Uri,
    string Name,
    DateTime Created,
    DateTime LastModified,
    long Length);