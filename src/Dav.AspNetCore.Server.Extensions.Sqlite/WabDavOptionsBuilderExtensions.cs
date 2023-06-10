using Dav.AspNetCore.Server.Locks;
using Dav.AspNetCore.Server.Store;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dav.AspNetCore.Server.Extensions.Sqlite;

public static class WabDavOptionsBuilderExtensions
{
    /// <summary>
    /// Adds locking via sqlite.
    /// </summary>
    /// <param name="builder">The web dav options builder.</param>
    /// <param name="configureOptions">Used to configure the sqlite lock options.</param>
    /// <returns>The web dav options builder.</returns>
    public static WebDavOptionsBuilder AddSqliteLocks(
        this WebDavOptionsBuilder builder,
        Action<SqlLockOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var options = new SqlLockOptions();
        configureOptions(options);
        
        builder.Services.TryAddSingleton(options);
        builder.AddStaleLocksRemovalJob();

        return builder.AddLocks<SqliteLockManager>();
    }

    /// <summary>
    /// Adds the sqlite property store.
    /// </summary>
    /// <param name="builder">The web dav options builder.</param>
    /// <param name="configureOptions">Used to configure the property store options.</param>
    /// <returns>The web dav options builder.</returns>
    public static WebDavOptionsBuilder AddSqlitePropertyStore(
        this WebDavOptionsBuilder builder,
        Action<SqlPropertyStoreOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        return builder.AddPropertyStore<SqlPropertyStoreOptions, SqlitePropertyStore>(configureOptions);
    }
}