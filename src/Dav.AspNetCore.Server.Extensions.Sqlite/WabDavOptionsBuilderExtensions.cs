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
        Action<SqliteOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var options = new SqliteOptions();
        configureOptions(options);
        
        builder.Services.TryAddSingleton(options);

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
        Action<SqlitePropertyStoreOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        return builder.AddPropertyStore<SqlitePropertyStoreOptions, SqlitePropertyStore>(configureOptions);
    }
}