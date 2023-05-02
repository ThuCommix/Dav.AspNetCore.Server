namespace Dav.AspNetCore.Server.Store.Files;

public class LocalFileStoreOptions : StoreOptions
{
    /// <summary>
    /// Gets or sets the root path.
    /// </summary>
    public string RootPath { get; set; } = DriveInfo.GetDrives()
        .First(x => x.DriveType == DriveType.Fixed).RootDirectory.FullName;
}