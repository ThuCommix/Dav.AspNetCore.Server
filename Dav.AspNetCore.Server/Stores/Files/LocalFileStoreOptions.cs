namespace Dav.AspNetCore.Server.Stores.Files;

public class LocalFileStoreOptions
{
    /// <summary>
    /// Gets or sets the root path.
    /// </summary>
    public string RootPath { get; set; } = DriveInfo.GetDrives()
        .First(x => x.DriveType == DriveType.Fixed).RootDirectory.FullName;
}