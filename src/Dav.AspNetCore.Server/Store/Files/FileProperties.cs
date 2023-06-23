namespace Dav.AspNetCore.Server.Store.Files;

public record FileProperties(
    ResourcePath Path,
    string Name,
    DateTime Created,
    DateTime LastModified,
    long Length);