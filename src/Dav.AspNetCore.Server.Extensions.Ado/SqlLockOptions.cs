namespace Dav.AspNetCore.Server.Extensions;

public class SqlLockOptions
{
    /// <summary>
    /// Gets the default resource lock table.
    /// </summary>
    public const string DefaultResourceLockTable = "dav_aspnetcore_server_resource_lock";

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
    public string Table { get; set; } = DefaultResourceLockTable;
}