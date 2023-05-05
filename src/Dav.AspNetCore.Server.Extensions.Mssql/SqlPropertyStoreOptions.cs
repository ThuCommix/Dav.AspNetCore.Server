using Dav.AspNetCore.Server.Store.Properties;

namespace Dav.AspNetCore.Server.Extensions.Mssql;

public class SqlPropertyStoreOptions : PropertyStoreOptions
{
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string? ConnectionString { get; set; }
}