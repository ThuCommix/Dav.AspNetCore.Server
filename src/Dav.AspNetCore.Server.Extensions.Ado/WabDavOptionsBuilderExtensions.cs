using Microsoft.Extensions.DependencyInjection;

namespace Dav.AspNetCore.Server.Extensions;

public static class WabDavOptionsBuilderExtensions
{
    /// <summary>
    /// Adds the stale lock removal job.
    /// </summary>
    /// <param name="builder">The web dav options builder.</param>
    /// <returns>The web dav options builder.</returns>
    public static WebDavOptionsBuilder AddStaleLocksRemovalJob(
        this WebDavOptionsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        
        builder.Services.AddHostedService<StaleLocksRemovalJob>();

        return builder;
    }
}