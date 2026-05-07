using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods for bridging disparate hosting environments into the standard 
/// <see cref="IHostEnvironment"/> abstraction.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Bridges an external hosting environment (like Blazor WebAssembly) into the standard 
    /// <see cref="IHostEnvironment"/> abstraction.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the bridge to.</param>
    /// <param name="environment">The name of the environment (e.g., "Development", "Production").</param>
    /// <param name="applicationName">The name of the application.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddHostEnvironmentBridge(
        this IServiceCollection services,
        string environment,
        string applicationName)
    {
        services.AddSingleton<IHostEnvironment>(new HostEnvironmentBridge(environment, applicationName));
        return services;
    }
}
