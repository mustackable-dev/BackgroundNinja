using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackgroundNinja;

/// <summary>
/// These are convenience extension methods for registering background
/// worker services. Since <see cref="BackgroundWorkerService"/> is a
/// public class, you are free to register the workers in any manner
/// that suits your application.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// This extension method adds a singleton background worker to your hosted services. If your
    /// application design requires multiple background workers, you can achieve this by calling this
    /// method multiple times.
    /// </summary>
    /// <param name="services">An IServiceCollection to add the worker to</param>
    /// <param name="operations">A collection with all the operations the worker will run. See <see cref="BackgroundOperation"/></param>
    /// <returns></returns>
    public static IServiceCollection AddBackgroundWorker(this IServiceCollection services, params BackgroundOperation[] operations)
    {
        services.AddSingleton<IHostedService, BackgroundWorkerService>(x => 
            new(operations, x.GetRequiredService<IServiceScopeFactory>()));
        return services;
    }
    
    /// <summary>
    /// This extension method adds a keyed singleton background worker to your hosted services. If your
    /// application design requires multiple background workers, you can achieve this by calling this
    /// method multiple times with unique keys.
    /// </summary>
    /// <param name="services">An IServiceCollection to add the worker to</param>
    /// <param name="key">A unique identifier for this worker service</param>
    /// <param name="operations">A collection with all the operations the worker will run. See <see cref="BackgroundOperation"/></param>
    /// <returns></returns>
    public static IServiceCollection AddKeyedBackgroundWorker(this IServiceCollection services, object? key, params BackgroundOperation[] operations)
    {
        services.AddKeyedSingleton<BackgroundWorkerService>(key, (x, _) => 
            new(operations, x.GetRequiredService<IServiceScopeFactory>()));
        services.AddSingleton<IHostedService, BackgroundWorkerService>(x=>
            x.GetRequiredKeyedService<BackgroundWorkerService>(key));
        return services;
    }
}