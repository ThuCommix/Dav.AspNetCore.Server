namespace Dav.AspNetCore.Server.Stores.Files;

public record DirectoryProperties(    
    Uri Uri,
    string Name,
    DateTime Created,
    DateTime LastModified);