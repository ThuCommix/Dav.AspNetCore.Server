using Dav.AspNetCore.Server.Store.Files;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dav.AspNetCore.Server.Store;

public static class WabDavOptionsBuilderExtensions
{
    /// <summary>
    /// Adds the local files store.
    /// </summary>
    /// <param name="builder">The web dav options builder.</param>
    /// <param name="configureOptions">Used to configure the store options.</param>
    /// <returns>The web dav options builder.</returns>
    public static WebDavOptionsBuilder AddLocalFiles(
        this WebDavOptionsBuilder builder, 
        Action<LocalFileStoreOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        builder.AddStore<LocalFileStoreOptions, LocalFileStore>(configureOptions);
        
        return builder;
    }

    /// <summary>
    /// Adds a store.
    /// </summary>
    /// <param name="builder">The web dav options builder.</param>
    /// <param name="configureOptions">Used to configure the store options.</param>
    /// <typeparam name="TOptions">The store options type.</typeparam>
    /// <typeparam name="TStore">The store type.</typeparam>
    /// <returns>The web dav options builder.</returns>
    public static WebDavOptionsBuilder AddStore<TOptions, TStore>(
        this WebDavOptionsBuilder builder,
        Action<TOptions>? configureOptions = null)
        where TOptions : StoreOptions, new() where TStore : class, IStore
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var storeOptions = new TOptions();
        if (configureOptions != null)
            configureOptions(storeOptions);

        builder.Services.Replace(ServiceDescriptor.Singleton(storeOptions));
        builder.Services.Replace(ServiceDescriptor.Scoped<IStore, TStore>());

        return builder;
    }
}