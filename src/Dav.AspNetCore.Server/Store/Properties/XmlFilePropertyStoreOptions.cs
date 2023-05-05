namespace Dav.AspNetCore.Server.Store.Properties;

public class XmlFilePropertyStoreOptions : PropertyStoreOptions
{
    /// <summary>
    /// Gets or sets the root path where the properties will be shared.
    /// </summary>
    public string RootPath { get; set; } = DriveInfo.GetDrives()
        .First(x => x.DriveType == DriveType.Fixed).RootDirectory.FullName;
}