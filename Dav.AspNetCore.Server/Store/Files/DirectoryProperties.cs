namespace Dav.AspNetCore.Server.Store.Files;

public record DirectoryProperties(    
    Uri Uri,
    string Name,
    DateTime Created,
    DateTime LastModified);