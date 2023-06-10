using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Store;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dav.AspNetCore.Server.Extensions.SqlServer;

public static class WabDavOptionsBuilderExtensions
{
    /// <summary>
    /// Adds locking via mssql.
    /// </summary>
    /// <param name="builder">The web dav options builder.</param>
    /// <param name="configureOptions">Used to configure the mssql lock options.</param>
    /// <returns>The web dav options builder.</returns>
    public static WebDavOptionsBuilder AddSqlLocks(
        this WebDavOptionsBuilder builder,
        Action<SqlLockOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var options = new SqlLockOptions();
        configureOptions(options);
        
        builder.Services.TryAddSingleton(options);
        builder.AddStaleLocksRemovalJob();

        return builder.AddLocks<SqlServerLockManager>();
    }

    /// <summary>
    /// Adds the mssql property store.
    /// </summary>
    /// <param name="builder">The web dav options builder.</param>
    /// <param name="configureOptions">Used to configure the property store options.</param>
    /// <returns>The web dav options builder.</returns>
    public static WebDavOptionsBuilder AddSqlPropertyStore(
        this WebDavOptionsBuilder builder,
        Action<SqlPropertyStoreOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        return builder.AddPropertyStore<SqlPropertyStoreOptions, SqlServerPropertyStore>(configureOptions);
    }
}