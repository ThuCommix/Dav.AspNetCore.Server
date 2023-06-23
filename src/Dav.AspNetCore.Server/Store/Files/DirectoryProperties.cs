namespace Dav.AspNetCore.Server.Store.Files;

public record DirectoryProperties(    
    ResourcePath Path,
    string Name,
    DateTime Created,
    DateTime LastModified);