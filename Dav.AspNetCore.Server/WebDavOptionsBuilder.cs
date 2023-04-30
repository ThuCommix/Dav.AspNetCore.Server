using Microsoft.Extensions.DependencyInjection;

namespace Dav.AspNetCore.Server;

public class WebDavOptionsBuilder : WebDavOptions
{
    /// <summary>
    /// Initializes a new <see cref="WebDavOptionsBuilder"/> class.
    /// </summary>
    /// <param name="services">The service collection.</param>
    internal WebDavOptionsBuilder(IServiceCollection services)
    {
        Services = services;
    }
    
    /// <summary>
    /// Gets the service collection.
    /// </summary>
    public IServiceCollection Services { get; }
}