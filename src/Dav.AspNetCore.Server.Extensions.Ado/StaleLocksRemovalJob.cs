using Dav.AspNetCore.Server.Locks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dav.AspNetCore.Server.Extensions;

internal class StaleLocksRemovalJob : BackgroundService
{
    private readonly IServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new <see cref="StaleLocksRemovalJob"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public StaleLocksRemovalJob(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        this.serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
    /// the lifetime of the long running operation(s) being performed.
    /// </summary>
    /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.</returns>
    /// <remarks>See <see href="https://docs.microsoft.com/dotnet/core/extensions/workers">Worker Services in .NET</see> for implementation guidelines.</remarks>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                using var scope = serviceProvider.CreateScope();

                var lockManager = scope.ServiceProvider.GetService<ILockManager>();
                if (lockManager == null)
                    continue;

                if (lockManager is SqlLockManager sqlLockManager)
                {
                    await sqlLockManager.RemoveStaleLocksAsync();
                }

            }
        }, stoppingToken);
    }
}