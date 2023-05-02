using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dav.AspNetCore.Server.Locks;

public static class WebDavOptionsBuilderExtensions
{
    public static WebDavOptionsBuilder AddInMemoryLocks(this WebDavOptionsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        
        builder.Services.Replace(ServiceDescriptor.Singleton<ILockManager>(new InMemoryLockManager(Array.Empty<ResourceLock>())));

        return builder;
    }

    public static WebDavOptionsBuilder AddLocks<T>(this WebDavOptionsBuilder builder) where T : class, ILockManager
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        builder.Services.Replace(ServiceDescriptor.Singleton<ILockManager, T>());
        
        return builder;
    }
}