using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dav.AspNetCore.Server.Store.Files;

public static class LocalFileStoreWebDavBuilderExtensions
{
    public static WebDavOptionsBuilder AddLocalFiles(
        this WebDavOptionsBuilder builder, 
        Action<LocalFileStoreOptions>? optionsBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        var options = new LocalFileStoreOptions();
        if (optionsBuilder != null)
            optionsBuilder(options);
        
        builder.Services.TryAddSingleton(options);
        builder.Services.TryAddScoped<IStore, LocalFileStore>();
        
        return builder;
    }
}