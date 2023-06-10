using Dav.AspNetCore.Server.Store.Properties;

namespace Dav.AspNetCore.Server.Extensions;

public class SqlPropertyStoreOptions : PropertyStoreOptions
{
    /// <summary>
    /// Gets the default property store table.
    /// </summary>
    public const string DefaultPropertyStoreTable = "dav_aspnetcore_server_property";
    
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the database schema.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets the database table name.
    /// </summary>
    public string Table { get; set; } = DefaultPropertyStoreTable;
}